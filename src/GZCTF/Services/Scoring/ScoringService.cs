using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace GZCTF.Services.Scoring;

public class ScoringService
{
    private readonly AppDbContext _context;
    private readonly IChallengeFlagRepository _flagRepo;
    private readonly IChallengePenaltyRepository _penaltyRepo;
    private readonly ISubmissionRecordRepository _recordRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IBigScreenEventRepository _eventRepo;
    private readonly IGameNoticeRepository _noticeRepo;
    private readonly CompetitionSettingsService _settings;
    private readonly IHubContext<BigScreenHub, IBigScreenClient> _bigScreenHub;
    private readonly IStringLocalizer<Program> _localizer;

    public ScoringService(
        AppDbContext context,
        IChallengeFlagRepository flagRepo,
        IChallengePenaltyRepository penaltyRepo,
        ISubmissionRecordRepository recordRepo,
        ISubmissionRepository submissionRepo,
        IBigScreenEventRepository eventRepo,
        IGameNoticeRepository noticeRepo,
        CompetitionSettingsService settings,
        IHubContext<BigScreenHub, IBigScreenClient> bigScreenHub,
        IStringLocalizer<Program> localizer)
    {
        _context = context;
        _flagRepo = flagRepo;
        _penaltyRepo = penaltyRepo;
        _recordRepo = recordRepo;
        _submissionRepo = submissionRepo;
        _eventRepo = eventRepo;
        _noticeRepo = noticeRepo;
        _settings = settings;
        _bigScreenHub = bigScreenHub;
        _localizer = localizer;
    }

    /// <summary>
    /// Process a flag submission and determine the score change.
    /// Must be called within an existing database transaction.
    /// </summary>
    public async Task<ScoreResult> ProcessSubmission(
        GameChallenge challenge,
        Participation participation,
        string answer,
        CancellationToken token = default)
    {
        // 1. Try to match against ChallengeFlags
        var flags = await _flagRepo.GetFlags(challenge.Id, token);

        var matchedFlag = flags.FirstOrDefault(f =>
            string.Equals(f.FlagValue, answer, StringComparison.Ordinal));

        if (matchedFlag is not null)
            return await HandleCorrectSubmission(challenge, participation, matchedFlag, token);

        // 2. Check for duplicate wrong answer — same answer previously submitted
        var duplicate = await _context.Submissions
            .AsNoTracking()
            .AnyAsync(s => s.ParticipationId == participation.Id
                && s.ChallengeId == challenge.Id
                && s.Answer == answer, token);

        if (duplicate)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                Message = "Duplicate submission, no penalty applied"
            };
        }

        return await HandleWrongSubmission(challenge, participation, token);
    }

    private async Task<ScoreResult> HandleCorrectSubmission(
        GameChallenge challenge,
        Participation participation,
        ChallengeFlag matchedFlag,
        CancellationToken token)
    {
        // Check if already submitted this flag
        var alreadySubmitted = await _recordRepo.HasCorrectSubmission(
            challenge.Id, participation.Id, matchedFlag.Id, token);

        if (alreadySubmitted)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                MatchedFlagId = matchedFlag.Id,
                Message = "This flag has already been submitted"
            };
        }

        // Determine blood bonus type
        var submissionType = await DetermineSubmissionType(challenge, participation, token);

        return new ScoreResult
        {
            Status = AnswerResult.Accepted,
            ScoreChange = matchedFlag.Score,
            MatchedFlagId = matchedFlag.Id,
            SubmissionType = submissionType
        };
    }

    /// <summary>
    /// Determine the submission type (FirstBlood, SecondBlood, ThirdBlood, Normal)
    /// and create the FirstSolves entry. Must be called within a transaction.
    /// </summary>
    private async Task<SubmissionType> DetermineSubmissionType(
        GameChallenge challenge,
        Participation participation,
        CancellationToken token)
    {
        // Check if already solved by this participation
        var alreadySolved = await _context.FirstSolves
            .AnyAsync(fs => fs.ParticipationId == participation.Id &&
                            fs.ChallengeId == challenge.Id, token);

        if (alreadySolved)
            return SubmissionType.Normal;

        // Load participation with division for permission check
        var part = await _context.Participations
            .IgnoreAutoIncludes().AsNoTracking()
            .Include(p => p.Division)
            .ThenInclude(d => d!.ChallengeConfigs)
            .SingleAsync(p => p.Id == participation.Id, token);

        var time = await _context.Games.IgnoreAutoIncludes().AsNoTracking()
            .Where(g => g.Id == part.GameId)
            .Select(g => new { g.StartTimeUtc, g.EndTimeUtc })
            .SingleAsync(token);

        var now = DateTimeOffset.UtcNow;

        var withinGameWindow = now >= time.StartTimeUtc && now < time.EndTimeUtc;
        var withinDeadline = !challenge.DeadlineUtc.HasValue || now <= challenge.DeadlineUtc.Value;

        var hasBloodPermission = withinGameWindow && withinDeadline && !challenge.DisableBloodBonus &&
                                 HasBloodPermission(part.Division, challenge.Id);

        if (!hasBloodPermission)
            return SubmissionType.Normal;

        var bloodEligibleCount = await CountBloodEligibleSolves(challenge.Id, time.StartTimeUtc,
            time.EndTimeUtc, token);

        return bloodEligibleCount switch
        {
            0 => SubmissionType.FirstBlood,
            1 => SubmissionType.SecondBlood,
            2 => SubmissionType.ThirdBlood,
            _ => SubmissionType.Normal
        };
    }

    private async Task<int> CountBloodEligibleSolves(int challengeId, DateTimeOffset start, DateTimeOffset end,
        CancellationToken token)
    {
        var eligibleParticipationIds =
            from participation in _context.Participations.AsNoTracking()
            join config in _context.Set<DivisionChallengeConfig>().AsNoTracking()
                    .Where(c => c.ChallengeId == challengeId)
                on participation.DivisionId equals config.DivisionId into configJoin
            from cfg in configJoin.DefaultIfEmpty()
            join division in _context.Divisions.AsNoTracking()
                on participation.DivisionId equals division.Id into divisionJoin
            from div in divisionJoin.DefaultIfEmpty()
            where participation.Status == ParticipationStatus.Accepted
                  && (cfg != null
                      ? cfg.Permissions.HasFlag(GamePermission.GetBlood)
                      : div == null || div.DefaultPermissions.HasFlag(GamePermission.GetBlood))
            select participation.Id;

        return await (
            from fs in _context.FirstSolves.AsNoTracking()
            join submission in _context.Submissions.AsNoTracking() on fs.SubmissionId equals submission.Id
            where fs.ChallengeId == challengeId
                  && eligibleParticipationIds.Contains(fs.ParticipationId)
                  && submission.SubmitTimeUtc >= start
                  && submission.SubmitTimeUtc < end
            orderby submission.SubmitTimeUtc
            select fs.ParticipationId
        ).Take(4).CountAsync(token);
    }

    private static bool HasBloodPermission(Division? division, int challengeId)
    {
        if (division is null)
            return true;

        var specific = division.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challengeId);
        var permissions = specific?.Permissions ?? division.DefaultPermissions;
        return permissions.HasFlag(GamePermission.GetBlood);
    }

    /// <summary>
    /// Create a FirstSolves entry after the submission has been saved.
    /// Only creates the entry when ALL flags for the challenge have been submitted correctly.
    /// </summary>
    public async Task CreateFirstSolve(int participationId, int challengeId, int submissionId,
        CancellationToken token = default)
    {
        var exists = await _context.FirstSolves
            .AnyAsync(fs => fs.ParticipationId == participationId && fs.ChallengeId == challengeId, token);

        if (exists)
            return;

        // Only mark as solved when all flags for this challenge have been submitted
        var totalFlags = await _context.ChallengeFlags
            .CountAsync(f => f.ChallengeId == challengeId, token);

        var submittedFlags = await _context.SubmissionRecords
            .Where(r => r.MatchedFlagId != null
                && r.Submission!.ParticipationId == participationId
                && r.Submission!.ChallengeId == challengeId)
            .Select(r => r.MatchedFlagId)
            .Distinct()
            .CountAsync(token);

        if (submittedFlags >= totalFlags)
        {
            _context.FirstSolves.Add(new FirstSolve
            {
                ParticipationId = participationId,
                ChallengeId = challengeId,
                SubmissionId = submissionId
            });
            await _context.SaveChangesAsync(token);
        }
    }

    private async Task<ScoreResult> HandleWrongSubmission(
        GameChallenge challenge,
        Participation participation,
        CancellationToken token)
    {
        var penalty = await _penaltyRepo.GetPenalty(challenge.Id, token);
        var defaultPenaltyScore = await _settings.Get<int>("Scoring.DefaultPenaltyScore", token);

        // Fall back to global default if no per-challenge penalty is configured or score is 0
        var penaltyScore = penalty?.PenaltyScore ?? defaultPenaltyScore;
        if (penaltyScore <= 0)
            penaltyScore = defaultPenaltyScore;

        if (penaltyScore <= 0)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0
            };
        }

        var minScore = await _settings.Get<int>("Scoring.MinScore", token);
        var maxPenalty = await _settings.Get<int>("Scoring.MaxPenaltyPerSubmission", token);

        var effectivePenalty = Math.Min(penaltyScore, maxPenalty);

        // Calculate current combined total score (GZCTF base + SubmissionRecords)
        var gzctfBaseScore = await _recordRepo.GetGZCTFBaseScore(participation.Id, participation.GameId, token);
        var currentExtraScore = await _recordRepo.GetTotalScoreChange(participation.Id, token);
        var combinedTotal = gzctfBaseScore + currentExtraScore;

        // Ensure combined score doesn't go below MinScore
        var actualPenalty = Math.Min(effectivePenalty, combinedTotal - minScore);
        if (actualPenalty <= 0)
            actualPenalty = 0;

        return new ScoreResult
        {
            Status = AnswerResult.WrongAnswer,
            ScoreChange = -actualPenalty,
            IsPenalty = actualPenalty > 0
        };
    }

    /// <summary>
    /// Create a SubmissionRecord for the processed submission.
    /// </summary>
    public async Task<SubmissionRecord> CreateRecord(
        Submission submission,
        ScoreResult result,
        CancellationToken token = default)
    {
        var record = new SubmissionRecord
        {
            SubmissionId = submission.Id,
            ScoreChange = result.ScoreChange,
            IsPenalty = result.IsPenalty,
            MatchedFlagId = result.MatchedFlagId
        };

        return await _recordRepo.CreateRecord(record, token);
    }

    /// <summary>
    /// Approve a ticket: write SubmissionRecord for refund/bonus and create BigScreenEvent.
    /// </summary>
    public async Task ApproveTicket(Ticket ticket, int refundScore, int bonusScore,
        CancellationToken token = default)
    {
        var totalScore = refundScore + bonusScore;
        if (totalScore <= 0 || ticket.Submission is null)
            return;

        var sub = ticket.Submission;

        var approvalSubmission = new Submission
        {
            Answer = $"[Ticket #{ticket.Id}]",
            Status = AnswerResult.Accepted,
            UserId = ticket.UserId,
            TeamId = sub.TeamId,
            ParticipationId = sub.ParticipationId,
            GameId = sub.GameId,
            ChallengeId = sub.ChallengeId,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        approvalSubmission = await _submissionRepo.AddSubmission(approvalSubmission, token);

        var record = new SubmissionRecord
        {
            SubmissionId = approvalSubmission.Id,
            ScoreChange = totalScore,
            IsPenalty = false
        };

        await _recordRepo.CreateRecord(record, token);

        var enableBonus = await _settings.Get<bool>("BigScreen.EnableBonusEvents", token);
        if (enableBonus && totalScore > 0)
        {
            var evt = new BigScreenEvent
            {
                EventType = BigScreenEventType.Bonus,
                ChallengeId = sub.ChallengeId,
                UserId = ticket.UserId,
                TeamName = sub.Team?.Name ?? "",
                ChallengeName = sub.GameChallenge?.Title ?? "",
                ScoreChange = totalScore,
                Message = $"{sub.Team?.Name ?? ""} 获得工单返还 +{refundScore} 分"
                    + (bonusScore > 0 ? $"，额外加分 +{bonusScore} 分" : "")
                    + $"（题目：{sub.GameChallenge?.Title ?? ""}）"
            };

            evt = await _eventRepo.CreateEvent(evt, token);

            var dto = new BigScreenEventDto
            {
                Id = evt.Id,
                EventType = evt.EventType.ToString(),
                Message = evt.Message,
                TeamName = evt.TeamName,
                ChallengeName = evt.ChallengeName,
                ScoreChange = evt.ScoreChange,
                CreatedAt = evt.CreatedAt
            };

            await _bigScreenHub.Clients.Group("BigScreen").ReceiveEvent(dto);
        }
    }
}

public class ScoreResult
{
    public AnswerResult Status { get; init; }
    public int ScoreChange { get; init; }
    public Guid? MatchedFlagId { get; init; }
    public bool IsPenalty { get; init; }
    public string? Message { get; init; }
    public SubmissionType SubmissionType { get; init; }
}
