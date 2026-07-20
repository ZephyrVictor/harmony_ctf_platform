using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ZeroDayTicketRepository(
    AppDbContext context,
    IGameNoticeRepository gameNoticeRepository,
    ChallengeScoreService challengeScoreService) : RepositoryBase(context), IZeroDayTicketRepository
{
    public async Task<ZeroDayTicket> CreateTicket(ZeroDayTicket ticket, CancellationToken token = default)
    {
        await Context.ZeroDayTickets.AddAsync(ticket, token);
        await Context.SaveChangesAsync(token);
        return await GetTicket(ticket.Id, token) ?? ticket;
    }

    public Task<ZeroDayTicket?> GetTicket(Guid ticketId, CancellationToken token = default)
        => Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Include(t => t.User)
            .Include(t => t.Reviewer)
            .FirstOrDefaultAsync(t => t.Id == ticketId, token);

    public Task<ZeroDayTicket[]> GetUserTickets(Guid userId, int? gameId = null, int count = 100, int skip = 0,
        CancellationToken token = default)
    {
        IQueryable<ZeroDayTicket> query = Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Where(t => t.UserId == userId);

        if (gameId.HasValue)
            query = query.Where(t => t.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<ZeroDayTicket[]> GetAllTickets(ZeroDayTicketStatus? status = null, int? gameId = null,
        int count = 100, int skip = 0, CancellationToken token = default)
    {
        IQueryable<ZeroDayTicket> query = Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Include(t => t.User)
            .Include(t => t.Reviewer);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (gameId.HasValue)
            query = query.Where(t => t.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    /// <summary>
    /// 审核通过 0day 工单 (R17 + R18 + R24):
    /// 1. 更新工单状态 + BonusScore
    /// 2. R24 跨队 0day 重复检测：若同一漏洞 (FilePath+LineNumber) 已被他队 approve 的 0day 工单占用，拒绝
    /// 3. 若工单含结构化字段 (FilePath + LineNumber + CommitHash + CWE)，创建新 Track + 3 Flags (L1/L2/L3) — R18 闭环
    /// 4. 创建 SubmissionRecord (ScoreChange=BonusScore) 加分到提交者队伍 — R17
    /// 若 admin 通过 reviewChallengeId 指定了 ChallengeId, 会覆盖工单上的 ChallengeId (用于工单无 ChallengeId 的情况)
    /// </summary>
    public async Task<ZeroDayTicket> ApproveTicket(Guid ticketId, int bonusScore, string? comment,
        Guid reviewerId, int? reviewChallengeId = null, CancellationToken token = default)
    {
        var ticket = await Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .FirstOrDefaultAsync(t => t.Id == ticketId, token)
            ?? throw new InvalidOperationException("Ticket not found");

        // R24: 跨队 0day 重复检测 — 同一 FilePath+LineNumber 只能有一个 approve 的 0day 工单
        // 后续队伍提交同一漏洞的 0day 工单应被拒绝（或只能作为 Nday 提交得分，无 0day bonus）
        if (!string.IsNullOrWhiteSpace(ticket.FilePath) && !string.IsNullOrWhiteSpace(ticket.LineNumber))
        {
            var duplicateApproved = await Context.ZeroDayTickets
                .AsNoTracking()
                .AnyAsync(t => t.Id != ticket.Id
                              && t.GameId == ticket.GameId
                              && t.Status == ZeroDayTicketStatus.Approved
                              && t.FilePath == ticket.FilePath
                              && t.LineNumber == ticket.LineNumber, token);
            if (duplicateApproved)
            {
                throw new InvalidOperationException(
                    "R24: This 0day vulnerability has already been approved for another team. "
                    + "Subsequent submitters can only score as Nday, no 0day bonus.");
            }
        }

        // 若 admin 指定了 ChallengeId, 更新工单 (用于工单无 ChallengeId 的情况, 确保 bonus 能加分)
        if (reviewChallengeId is int cid && ticket.ChallengeId is null)
        {
            var challenge = await Context.GameChallenges
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cid && c.GameId == ticket.GameId, token);
            if (challenge is null)
                throw new InvalidOperationException($"Challenge {cid} not found in game {ticket.GameId}");
            ticket.ChallengeId = cid;
        }

        ticket.Status = ZeroDayTicketStatus.Approved;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = comment;
        ticket.BonusScore = bonusScore;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        // R18: 0day → Nday 闭环 — 创建新 Track + 3 Flags
        if (ticket.ChallengeId is not null
            && !string.IsNullOrWhiteSpace(ticket.FilePath)
            && !string.IsNullOrWhiteSpace(ticket.LineNumber)
            && !string.IsNullOrWhiteSpace(ticket.CommitHash)
            && !string.IsNullOrWhiteSpace(ticket.CWE))
        {
            var createdTrackId = await CreateTrackFromTicket(ticket, token);
            ticket.CreatedTrackId = createdTrackId;
        }

        await Context.SaveChangesAsync(token);

        // R17: 0day bonus 加分到提交者队伍
        if (bonusScore > 0)
        {
            await AwardBonusToSubmitter(ticket, bonusScore, comment, reviewerId, token);
        }

        return await GetTicket(ticketId, token) ?? ticket;
    }

    /// <summary>
    /// 从工单数据创建新 Track + 3 Flags (L1=file:line, L2=commit, L3=CWE) + LevelConfigs.
    /// </summary>
    private async Task<Guid> CreateTrackFromTicket(ZeroDayTicket ticket, CancellationToken token)
    {
        var challengeId = ticket.ChallengeId!.Value;

        // 检查同 FilePath 的 track 是否已存在（避免重复创建）
        var existingTrack = await Context.Set<ChallengeTrack>()
            .FirstOrDefaultAsync(t => t.ChallengeId == challengeId && t.FilePath == ticket.FilePath, token);
        if (existingTrack is not null)
            return existingTrack.Id;

        var track = new ChallengeTrack
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            Title = string.IsNullOrWhiteSpace(ticket.Title) ? ticket.FilePath : ticket.Title,
            Repository = null,
            Module = ticket.Module,
            FilePath = ticket.FilePath,
            Category = "0day",
            ReferenceUrl = null,
            IsEnabled = true,
            SortOrder = 9999  // 0day 创建的 track 排在后面
        };
        await Context.Set<ChallengeTrack>().AddAsync(track, token);
        await Context.SaveChangesAsync(token);

        // L1 = FilePath:LineNumber (e.g., "path/to/file.cpp:c:1060")
        var l1Value = $"{ticket.FilePath}:{ticket.LineNumber}";
        await CreateFlagAndLevelConfig(challengeId, track.Id, 1, l1Value,
            ChallengeFlagAnswerType.Exact, 5, token);

        // L2 = CommitHash (ban-trigger: "0" not allowed for 0day since it's a real fix)
        await CreateFlagAndLevelConfig(challengeId, track.Id, 2, ticket.CommitHash!,
            ChallengeFlagAnswerType.Exact, 4, token);

        // L3 = CWE
        await CreateFlagAndLevelConfig(challengeId, track.Id, 3, ticket.CWE!,
            ChallengeFlagAnswerType.Exact, 1, token);

        await challengeScoreService.SyncOriginalScoreFromLevels(challengeId, token);

        return track.Id;
    }

    private async Task CreateFlagAndLevelConfig(
        int challengeId, Guid trackId, int level, string flagValue,
        ChallengeFlagAnswerType answerType, int score, CancellationToken token)
    {
        var flag = new ChallengeFlag
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            FlagValue = flagValue,
            AnswerType = answerType,
            RawAnswer = flagValue,
            NormalizedAnswerKey = ScoringService.NormalizeHybridLineAnswer(flagValue, answerType),
            Level = level,
            TrackId = trackId,
            IsEnabled = true,
            IsVisible = true,
            SortOrder = 0
        };
        await Context.Set<ChallengeFlag>().AddAsync(flag, token);

        // Upsert LevelConfig (skip if exists)
        var existing = await Context.Set<ChallengeLevelConfig>()
            .FirstOrDefaultAsync(c => c.ChallengeId == challengeId && c.Level == level, token);
        if (existing is null)
        {
            await Context.Set<ChallengeLevelConfig>().AddAsync(new ChallengeLevelConfig
            {
                ChallengeId = challengeId,
                Level = level,
                Title = $"Level {level}",
                Score = score,
                PenaltyScore = 0,
                LockOnAnswers = [],
                BanOnAnswers = [],
                IsEnabled = true,
                SortOrder = level
            }, token);
        }

        await Context.SaveChangesAsync(token);
    }

    /// <summary>
    /// 创建 SubmissionRecord 把 BonusScore 加到提交者队伍的总分。
    /// </summary>
    private async Task AwardBonusToSubmitter(ZeroDayTicket ticket, int bonusScore, string? comment,
        Guid reviewerId, CancellationToken token)
    {
        // 找到提交者的 participation
        var participation = await Context.Participations
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.GameId == ticket.GameId
                && p.Members.Any(m => m.UserId == ticket.UserId), token);
        if (participation is null)
            return;  // 用户未加入比赛，跳过加分

        // 找到题目（用于 ChallengeId）；若工单没绑题目，用 0day 创建的 track 所在题目
        var challengeId = ticket.ChallengeId;
        if (challengeId is null && ticket.CreatedTrackId is not null)
        {
            challengeId = await Context.Set<ChallengeTrack>()
                .Where(t => t.Id == ticket.CreatedTrackId.Value)
                .Select(t => t.ChallengeId)
                .FirstOrDefaultAsync(token);
        }
        if (challengeId is null)
            return;

        // 创建一条 Submission (用于审计)
        var submission = new Submission
        {
            Answer = $"[0day Ticket #{ticket.Id}] bonus={bonusScore}",
            Status = AnswerResult.Accepted,
            UserId = ticket.UserId,
            TeamId = participation.TeamId,
            ParticipationId = participation.Id,
            GameId = ticket.GameId,
            ChallengeId = challengeId.Value,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };
        await Context.Submissions.AddAsync(submission, token);
        await Context.SaveChangesAsync(token);

        // 创建 SubmissionRecord (ScoreChange=bonusScore) — 这会自动影响 scoreboard
        var record = new SubmissionRecord
        {
            SubmissionId = submission.Id,
            ScoreChange = bonusScore,
            IsPenalty = false,
            GameId = ticket.GameId,
            ChallengeId = challengeId,
            ParticipationId = participation.Id,
            SubmissionType = "ZeroDayBonus"
        };
        await Context.SubmissionRecords.AddAsync(record, token);

        var reviewerName = await Context.Users
            .Where(u => u.Id == reviewerId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(token) ?? "Admin";
        var reason = string.IsNullOrWhiteSpace(comment)
            ? $"0day ticket approved: {ticket.Title}"
            : comment.Trim();

        await gameNoticeRepository.AddNotice(new GameNotice
        {
            GameId = ticket.GameId,
            Type = NoticeType.ScoreAdjustment,
            Values = new List<string>
            {
                participation.Team.Name,
                bonusScore > 0 ? $"+{bonusScore}" : bonusScore.ToString(),
                reason,
                reviewerName
            },
            PublishTimeUtc = DateTimeOffset.UtcNow
        }, token);
    }

    public async Task<ZeroDayTicket> RejectTicket(Guid ticketId, string reason, Guid reviewerId,
        CancellationToken token = default)
    {
        var ticket = await Context.ZeroDayTickets.FindAsync([ticketId], token)
                     ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = ZeroDayTicketStatus.Rejected;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = reason;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        await Context.SaveChangesAsync(token);
        return await GetTicket(ticketId, token) ?? ticket;
    }
}
