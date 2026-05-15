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
        var activeFlags = flags.Where(f => f.IsEnabled).ToArray();

        var matchedFlag = challenge.FlagMatchMode switch
        {
            FlagMatchMode.LineRange => MatchLineRange(activeFlags, answer),
            FlagMatchMode.HybridLine => await MatchHybridLine(challenge, participation, activeFlags, answer, token),
            _ => activeFlags.FirstOrDefault(f =>
                string.Equals(f.FlagValue, answer, StringComparison.Ordinal))
        };

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

        // HybridLine uses SubmissionRecords as the authoritative per-flag score ledger.
        var submissionType = challenge.FlagMatchMode == FlagMatchMode.HybridLine
            ? SubmissionType.Unaccepted
            : await DetermineSubmissionType(challenge, participation, token);

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
            .CountAsync(f => f.ChallengeId == challengeId && f.IsEnabled, token);

        var submittedFlags = await _context.SubmissionRecords
            .Where(r => r.MatchedFlagId != null
                && r.ScoreChange > 0
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

    static ChallengeFlag? MatchLineRange(ChallengeFlag[] flags, string answer)
    {
        if (!TryParseLineRange(answer, out var subStart, out var subEnd))
            return null;

        foreach (var flag in flags)
        {
            if (!TryParseLineRange(flag.FlagValue, out var flagStart, out var flagEnd))
                continue;

            if (subStart >= flagStart && subEnd <= flagEnd)
                return flag;
        }

        return null;
    }

    private async Task<ChallengeFlag?> MatchHybridLine(GameChallenge challenge, Participation participation,
        ChallengeFlag[] flags, string answer, CancellationToken token)
    {
        var submitted = ParseHybridLineAnswer(answer, ChallengeFlagAnswerType.Exact);
        if (submitted is null)
            throw new InvalidFlagSubmissionException(
                "Invalid line answer format. Use a single line, a range, or a comma-separated multi-line answer.");

        var completedFlagIds = await _context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.MatchedFlagId != null
                        && r.ScoreChange > 0
                        && (r.ParticipationId == participation.Id
                            || (r.ParticipationId == null
                                && r.Submission != null
                                && r.Submission.ParticipationId == participation.Id))
                        && (r.ChallengeId == challenge.Id
                            || (r.ChallengeId == null
                                && r.Submission != null
                                && r.Submission.ChallengeId == challenge.Id)))
            .Select(r => r.MatchedFlagId!.Value)
            .ToArrayAsync(token);

        HashSet<Guid> completed = [.. completedFlagIds];
        List<ChallengeFlag> openMatches = [];
        List<ChallengeFlag> completedMatches = [];

        foreach (var flag in flags)
        {
            var flagAnswer = ParseHybridLineAnswer(
                string.IsNullOrWhiteSpace(flag.RawAnswer) ? flag.FlagValue : flag.RawAnswer,
                flag.AnswerType);

            if (flagAnswer is null)
                continue;

            if (!HybridLineMatches(flagAnswer.Value, submitted.Value))
                continue;

            if (completed.Contains(flag.Id))
                completedMatches.Add(flag);
            else
                openMatches.Add(flag);
        }

        var openMatch = openMatches.Count switch
        {
            0 => null,
            1 => openMatches[0],
            _ => throw new InvalidFlagSubmissionException(
                "Submission matches multiple enabled flags in this subsystem; please fix flag configuration.")
        };

        if (openMatch is not null)
            return openMatch;

        return completedMatches.Count switch
        {
            0 => null,
            1 => completedMatches[0],
            _ => throw new InvalidFlagSubmissionException(
                "Submission matches multiple completed flags in this subsystem; please fix flag configuration.")
        };
    }

    public static bool TryNormalizeHybridLineAnswer(string input, ChallengeFlagAnswerType answerType,
        out string normalizedKey)
    {
        if (ParseHybridLineAnswer(input, answerType) is { } answer)
        {
            normalizedKey = answer.NormalizedKey;
            return true;
        }

        normalizedKey = string.Empty;
        return false;
    }

    public static string NormalizeHybridLineAnswer(string input, ChallengeFlagAnswerType answerType)
    {
        var answer = ParseHybridLineAnswer(input, answerType);
        return answer?.NormalizedKey ?? string.Empty;
    }

    private static HybridLineAnswer? ParseHybridLineAnswer(string input, ChallengeFlagAnswerType answerType)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return answerType switch
        {
            ChallengeFlagAnswerType.SingleLine => ParseSingleLine(input),
            ChallengeFlagAnswerType.LineRange => ParseLineRangeAnswer(input),
            ChallengeFlagAnswerType.MultiLine => ParseMultiLine(input),
            _ => InferHybridLineAnswer(input)
        };
    }

    private static HybridLineAnswer? InferHybridLineAnswer(string input)
    {
        var trimmed = input.Trim();

        if (ContainsMultiLineSeparator(trimmed))
            return ParseMultiLine(trimmed);

        return trimmed.Contains('-', StringComparison.Ordinal)
            ? ParseLineRangeAnswer(trimmed)
            : ParseSingleLine(trimmed);
    }

    private static HybridLineAnswer? ParseSingleLine(string input)
    {
        var token = StripLinePrefix(input.Trim());
        return int.TryParse(token, out var line) && line > 0
            ? HybridLineAnswer.Single(line)
            : null;
    }

    private static HybridLineAnswer? ParseLineRangeAnswer(string input)
    {
        var token = StripLinePrefix(input.Trim());
        var parts = token.Split('-', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return ParseSingleLine(input) is { } single ? HybridLineAnswer.Range(single.Start, single.End) : null;

        return int.TryParse(parts[0], out var start)
               && int.TryParse(parts[1], out var end)
               && start > 0
               && end >= start
            ? HybridLineAnswer.Range(start, end)
            : null;
    }

    private static HybridLineAnswer? ParseMultiLine(string input)
    {
        var parts = input.Trim()
            .Split(['、', ',', ';', '\r', '\n', '\t', ' '],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return null;

        List<int> lines = [];
        foreach (var part in parts)
        {
            var token = StripLinePrefix(part);
            if (token.Contains('-', StringComparison.Ordinal)
                || !int.TryParse(token, out var line)
                || line <= 0)
                return null;

            lines.Add(line);
        }

        var distinct = lines.Distinct().Order().ToArray();
        return distinct.Length == lines.Count
            ? HybridLineAnswer.Multi(distinct)
            : null;
    }

    private static bool HybridLineMatches(HybridLineAnswer flag, HybridLineAnswer submitted) =>
        flag.Kind switch
        {
            HybridLineAnswerKind.SingleLine => submitted.Kind == HybridLineAnswerKind.SingleLine
                                               && submitted.Start == flag.Start,
            HybridLineAnswerKind.LineRange => submitted.Kind switch
            {
                HybridLineAnswerKind.SingleLine => submitted.Start >= flag.Start && submitted.Start <= flag.End,
                HybridLineAnswerKind.LineRange => submitted.Start >= flag.Start && submitted.End <= flag.End,
                _ => false
            },
            HybridLineAnswerKind.MultiLine => submitted.Kind == HybridLineAnswerKind.MultiLine
                                              && submitted.Lines.SequenceEqual(flag.Lines),
            _ => false
        };

    private static bool ContainsMultiLineSeparator(string input) =>
        input.Any(c => c is '、' or ',' or ';' or '\r' or '\n' or '\t' or ' ');

    private static string StripLinePrefix(string token) =>
        token.StartsWith("c:", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;

    internal static bool TryParseLineRange(string input, out int start, out int end)
    {
        start = end = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();

        if (trimmed.StartsWith("c:", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];

        var parts = trimmed.Split('-', 2);
        if (parts.Length == 1)
        {
            if (!int.TryParse(parts[0], out start))
                return false;
            end = start;
            return start > 0;
        }

        if (!int.TryParse(parts[0], out start) || !int.TryParse(parts[1], out end))
            return false;

        return start > 0 && end >= start;
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
            MatchedFlagId = result.MatchedFlagId,
            GameId = submission.GameId,
            ChallengeId = submission.ChallengeId,
            ParticipationId = submission.ParticipationId
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
            IsPenalty = false,
            GameId = approvalSubmission.GameId,
            ChallengeId = approvalSubmission.ChallengeId,
            ParticipationId = approvalSubmission.ParticipationId
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

public class InvalidFlagSubmissionException(string message) : Exception(message);

internal enum HybridLineAnswerKind
{
    SingleLine,
    LineRange,
    MultiLine
}

internal readonly record struct HybridLineAnswer(
    HybridLineAnswerKind Kind,
    int Start,
    int End,
    int[] Lines)
{
    public string NormalizedKey => Kind switch
    {
        HybridLineAnswerKind.SingleLine => $"S:{Start}",
        HybridLineAnswerKind.LineRange => $"R:{Start}-{End}",
        HybridLineAnswerKind.MultiLine => $"M:{string.Join(',', Lines)}",
        _ => string.Empty
    };

    public static HybridLineAnswer Single(int line) => new(HybridLineAnswerKind.SingleLine, line, line, []);

    public static HybridLineAnswer Range(int start, int end) => new(HybridLineAnswerKind.LineRange, start, end, []);

    public static HybridLineAnswer Multi(int[] lines) =>
        new(HybridLineAnswerKind.MultiLine, lines.Length > 0 ? lines[0] : 0,
            lines.Length > 0 ? lines[^1] : 0, lines);
}
