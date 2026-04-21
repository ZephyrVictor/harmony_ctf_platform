using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Services.Scoring;

public class ScoringService
{
    private readonly IChallengeFlagRepository _flagRepo;
    private readonly IChallengePenaltyRepository _penaltyRepo;
    private readonly ISubmissionRecordRepository _recordRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IBigScreenEventRepository _eventRepo;
    private readonly CompetitionSettingsService _settings;
    private readonly IHubContext<BigScreenHub, IBigScreenClient> _bigScreenHub;

    public ScoringService(
        IChallengeFlagRepository flagRepo,
        IChallengePenaltyRepository penaltyRepo,
        ISubmissionRecordRepository recordRepo,
        ISubmissionRepository submissionRepo,
        IBigScreenEventRepository eventRepo,
        CompetitionSettingsService settings,
        IHubContext<BigScreenHub, IBigScreenClient> bigScreenHub)
    {
        _flagRepo = flagRepo;
        _penaltyRepo = penaltyRepo;
        _recordRepo = recordRepo;
        _submissionRepo = submissionRepo;
        _eventRepo = eventRepo;
        _settings = settings;
        _bigScreenHub = bigScreenHub;
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
                Message = "This flag has already been submitted"
            };
        }

        return new ScoreResult
        {
            Status = AnswerResult.Accepted,
            ScoreChange = matchedFlag.Score,
            MatchedFlagId = matchedFlag.Id
        };
    }

    private async Task<ScoreResult> HandleWrongSubmission(
        GameChallenge challenge,
        Participation participation,
        CancellationToken token)
    {
        var penalty = await _penaltyRepo.GetPenalty(challenge.Id, token);

        if (penalty is null || penalty.PenaltyScore <= 0)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0
            };
        }

        var minScore = await _settings.Get<int>("Scoring.MinScore", token);
        var maxPenalty = await _settings.Get<int>("Scoring.MaxPenaltyPerSubmission", token);

        var effectivePenalty = Math.Min(penalty.PenaltyScore, maxPenalty);

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
}
