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
    private readonly IChallengeTrackRepository _trackRepo;
    private readonly IChallengeLevelConfigRepository _levelConfigRepo;
    private readonly IChallengeLockStateRepository _lockStateRepo;
    private readonly IChallengeTrackLevelBanStateRepository _banStateRepo;
    private readonly IChallengeWrongCountStateRepository _wrongCountRepo;

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
        IStringLocalizer<Program> localizer,
        IChallengeTrackRepository trackRepo,
        IChallengeLevelConfigRepository levelConfigRepo,
        IChallengeLockStateRepository lockStateRepo,
        IChallengeTrackLevelBanStateRepository banStateRepo,
        IChallengeWrongCountStateRepository wrongCountRepo)
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
        _trackRepo = trackRepo;
        _levelConfigRepo = levelConfigRepo;
        _lockStateRepo = lockStateRepo;
        _banStateRepo = banStateRepo;
        _wrongCountRepo = wrongCountRepo;
    }

    /// <summary>
    /// 处理 flag 提交。必须在数据库事务内调用。
    /// </summary>
    public async Task<ScoreResult> ProcessSubmission(
        GameChallenge challenge,
        Participation participation,
        string answer,
        Guid? trackId,
        int level,
        CancellationToken token = default)
    {
        // 0. R11: 已取消参赛资格的队伍禁止提交
        if (participation.Status == ParticipationStatus.Suspended)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                Message = "Team disqualified (3 freeze strikes per R11)"
            };
        }

        // 1. 过期锁清理 + 计数重置
        var expiredLocks = await _lockStateRepo.GetExpiredLocks(participation.Id, challenge.Id, token);
        if (expiredLocks.Count > 0)
        {
            await _lockStateRepo.DeleteLocks(expiredLocks, token);
            await _wrongCountRepo.ResetAllLevels(participation.Id, challenge.Id, token);
        }

        // 2. 活跃锁检查
        var activeLock = await _lockStateRepo.GetActiveLock(participation.Id, challenge.Id, token);
        if (activeLock is not null)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                ChallengeLocked = true,
                UnlocksAt = activeLock.UnlocksAt,
                Message = $"Challenge locked until {activeLock.UnlocksAt:O} (reason: {activeLock.TriggerReason})"
            };
        }

        // 2.5 封禁检查（trap 自动封 + 管理员手动封）
        // 如果该 (Track, Level) 已被封禁，拒绝任何提交
        if (trackId is not null)
        {
            var isBanned = await _banStateRepo.IsBanned(
                participation.Id, challenge.Id, trackId.Value, level, token);
            if (isBanned)
            {
                return new ScoreResult
                {
                    Status = AnswerResult.WrongAnswer,
                    ScoreChange = 0,
                    BannedTrackId = trackId,
                    BannedLevel = level,
                    Message = "已被封禁，请联系管理员"
                };
            }
        }

        // 3. 匹配
        ChallengeFlag[] candidates;
        if (trackId is null)
        {
            // Level 1 入口：所有 Level 1 flags
            if (level != 1)
                return new ScoreResult
                {
                    Status = AnswerResult.WrongAnswer,
                    ScoreChange = 0,
                    Message = "trackId is required for Level > 1"
                };
            candidates = await _flagRepo.GetFlagsByLevel(challenge.Id, 1, token);
        }
        else
        {
            candidates = await _flagRepo.GetFlagsByTrackLevel(trackId.Value, level, token);
        }

        // 4. 找匹配的 flags
        var matchedFlags = candidates
            .Where(f => TryMatch(f, answer))
            .ToList();

        if (matchedFlags.Count == 0)
            return await HandleWrongSubmission(challenge, participation, answer, level, trackId, token);

        // 5. 分类匹配结果
        var firstValid = await PickFirstValidMatch(challenge, participation, matchedFlags, level, token);

        if (firstValid is null)
        {
            // 所有匹配都不可计分（已解/已封禁/锁定）
            // 不扣分，不泄漏具体原因
            // 但返回匹配到的 TrackId 以便前端知道该 flag 属于哪个 track（幂等场景）
            var matchedTrackId = matchedFlags.FirstOrDefault()?.TrackId ?? trackId;
            var matchedLevel = matchedFlags.FirstOrDefault()?.Level ?? level;

            return new ScoreResult
            {
                Status = AnswerResult.FlagSubmitted,
                ScoreChange = 0,
                TrackId = matchedTrackId,
                Level = matchedLevel,
                Message = "Answer not acceptable at this time"
            };
        }

        return await HandleCorrectSubmission(challenge, participation, firstValid, token);
    }

    private async Task<ChallengeFlag?> PickFirstValidMatch(
        GameChallenge challenge,
        Participation participation,
        List<ChallengeFlag> matchedFlags,
        int requestedLevel,
        CancellationToken token)
    {
        // 按 Level 升序、SortOrder 升序排序
        var ordered = matchedFlags.OrderBy(f => f.Level).ThenBy(f => f.SortOrder).ToList();

        foreach (var flag in ordered)
        {
            // 解锁检查
            if (flag.Level > 1)
            {
                var prevSolved = await _recordRepo.HasCorrectSubmission(
                    challenge.Id, participation.Id, flag.TrackId, flag.Level - 1, token);
                if (!prevSolved) continue;
            }

            // 已解检查（本队）— 同队重复不得分
            var alreadySolved = await _recordRepo.HasCorrectSubmission(
                challenge.Id, participation.Id, flag.TrackId, flag.Level, token);
            if (alreadySolved) continue;

            // 封禁检查
            var isBanned = await _banStateRepo.IsBanned(
                participation.Id, challenge.Id, flag.TrackId, flag.Level, token);
            if (isBanned) continue;

            return flag;
        }

        return null;
    }

    private static bool TryMatch(ChallengeFlag flag, string answer)
    {
        var submitted = ParseHybridLineAnswer(answer, flag.AnswerType);
        if (submitted is null)
        {
            // 不能解析时尝试 Exact 字面比对
            if (flag.AnswerType == ChallengeFlagAnswerType.Exact)
                return string.Equals(flag.FlagValue, answer, StringComparison.Ordinal);
            return false;
        }

        var flagAnswerStr = string.IsNullOrWhiteSpace(flag.RawAnswer) ? flag.FlagValue : flag.RawAnswer;
        var flagAnswer = ParseHybridLineAnswer(flagAnswerStr, flag.AnswerType);
        if (flagAnswer is null)
        {
            return string.Equals(flag.FlagValue, answer, StringComparison.Ordinal);
        }

        return HybridLineMatches(flagAnswer.Value, submitted.Value);
    }

    private async Task<ScoreResult> HandleCorrectSubmission(
        GameChallenge challenge,
        Participation participation,
        ChallengeFlag matchedFlag,
        CancellationToken token)
    {
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, matchedFlag.Level, token);
        var score = levelConfig?.Score ?? 0;

        var submissionType = await DetermineSubmissionType(challenge, participation, token);

        return new ScoreResult
        {
            Status = AnswerResult.Accepted,
            ScoreChange = score,
            MatchedFlagId = matchedFlag.Id,
            SubmissionType = submissionType,
            Level = matchedFlag.Level,
            TrackId = matchedFlag.TrackId,
            Message = "Accepted"
        };
    }

    private async Task<ScoreResult> HandleWrongSubmission(
        GameChallenge challenge,
        Participation participation,
        string answer,
        int level,
        Guid? trackId,
        CancellationToken token)
    {
        // 1. 加载 Level 配置（先加载以便检测 trap/ban）
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, level, token);

        var lockOnAnswers = levelConfig?.LockOnAnswers ?? [];
        var banOnAnswers = levelConfig?.BanOnAnswers ?? [];

        // 2. Trap / Ban 检测 — 必须在 dedup 之前，确保 ban 答案幂等生效
        bool triggerChallengeLock = false;
        string lockReason = "SubmissionLimitExceeded";
        Guid? bannedTrackId = null;
        int? bannedLevel = null;

        if (lockOnAnswers.Contains(answer))
        {
            triggerChallengeLock = true;
            lockReason = "TrapAnswer";
        }

        // BanOnAnswers 仅 Level 2+ 且有 trackId 时生效；幂等创建 ban state
        if (banOnAnswers.Contains(answer) && level > 1 && trackId is not null)
        {
            var alreadySolved = await _recordRepo.HasCorrectSubmission(
                challenge.Id, participation.Id, trackId.Value, level, token);
            if (!alreadySolved)
            {
                var alreadyBanned = await _banStateRepo.IsBanned(
                    participation.Id, challenge.Id, trackId.Value, level, token);
                if (!alreadyBanned)
                {
                    await _banStateRepo.CreateBan(new ChallengeTrackLevelBanState
                    {
                        ParticipationId = participation.Id,
                        ChallengeId = challenge.Id,
                        TrackId = trackId.Value,
                        Level = level,
                        BanAnswer = answer,
                        Reason = "Trap triggered",
                        BannedBy = null,
                        IsManual = false
                    }, token);
                }
                bannedTrackId = trackId;
                bannedLevel = level;
            }
        }

        // 3. Duplicate 检查：同一 answer 之前错误提交过 → 不扣分
        // 但 trap/ban 状态已经处理（幂等），所以这里仅控制是否再次扣分
        var duplicate = await _context.Submissions
            .AsNoTracking()
            .AnyAsync(s => s.ParticipationId == participation.Id
                && s.ChallengeId == challenge.Id
                && s.Answer == answer
                && s.Status == AnswerResult.WrongAnswer, token);

        if (duplicate && !triggerChallengeLock && bannedTrackId is null)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                TrackId = trackId,
                Level = level,
                BannedTrackId = bannedTrackId,
                BannedLevel = bannedLevel,
                Message = "Duplicate wrong submission, no penalty"
            };
        }

        // 4. Penalty 计算（重复提交不扣分）
        var penaltyScore = 0;
        if (!duplicate)
        {
            penaltyScore = levelConfig?.PenaltyScore ?? 0;
            if (penaltyScore <= 0)
            {
                var penalty = await _penaltyRepo.GetPenalty(challenge.Id, token);
                var defaultPenalty = await _settings.Get<int>("Scoring.DefaultPenaltyScore", token);
                penaltyScore = penalty?.PenaltyScore ?? defaultPenalty;
            }
        }

        // 5. WrongCount 累加 + SubmissionLimit 检查
        // 规则 R4: 防爆破维度 = per file path (Track)，跨所有 Level 累计错误提交数
        // 重复提交不累加（已 dedup）
        int wrongCount = 0;
        if (!duplicate)
        {
            wrongCount = await _wrongCountRepo.IncrementWrongCount(
                participation.Id, challenge.Id, level, token);

            if (!triggerChallengeLock)
            {
                var limit = levelConfig?.SubmissionLimit > 0
                    ? levelConfig.SubmissionLimit
                    : challenge.SubmissionLimit;

                if (limit > 0)
                {
                    int effectiveWrongCount;
                    if (trackId is not null)
                    {
                        // per-track 跨 level 累计（含本次 +1）
                        var historicalTrackWrong = await _context.SubmissionRecords
                            .AsNoTracking()
                            .CountAsync(r => r.ParticipationId == participation.Id
                                          && r.ChallengeId == challenge.Id
                                          && r.TrackId == trackId
                                          && r.IsPenalty, token);
                        effectiveWrongCount = historicalTrackWrong + 1;
                    }
                    else
                    {
                        // 无 Track 题目退化为 per-level 计数
                        effectiveWrongCount = wrongCount;
                    }

                    if (effectiveWrongCount >= limit)
                    {
                        triggerChallengeLock = true;
                        lockReason = "SubmissionLimitExceeded";
                    }
                }
            }
        }

        // 6. 创建全题锁
        // 规则 R10: 防爆破冻结"当日"提交窗口。LockDurationMinutes=0 时使用 24h 默认（避免无限锁）。
        // 管理员可通过 challenge.LockDurationMinutes 显式配置（含 0 表示使用默认 24h）。
        DateTimeOffset? unlocksAt = null;
        if (triggerChallengeLock)
        {
            const int defaultLockMinutes = 24 * 60;
            var lockMinutes = challenge.LockDurationMinutes > 0
                ? challenge.LockDurationMinutes
                : defaultLockMinutes;
            unlocksAt = DateTimeOffset.UtcNow.AddMinutes(lockMinutes);

            await _lockStateRepo.CreateLock(new ChallengeLockState
            {
                ParticipationId = participation.Id,
                ChallengeId = challenge.Id,
                TriggeringLevel = level,
                TriggeringTrackId = trackId,
                TriggerReason = lockReason,
                TriggerAnswer = answer,
                LockedAt = DateTimeOffset.UtcNow,
                UnlocksAt = unlocksAt.Value
            }, token);

            // R11: 3-strike disqualification — 累计 3 次冻结自动取消参赛资格
            var freezeCount = await _context.SubmissionRecords
                .AsNoTracking()
                .CountAsync(r => r.ParticipationId == participation.Id
                              && r.SubmissionType == "Trap", token);
            // 包含本次（即将创建的 Trap 记录）+1
            if (freezeCount + 1 >= 3)
            {
                var part = await _context.Participations
                    .FirstOrDefaultAsync(p => p.Id == participation.Id, token);
                if (part is not null && part.Status != ParticipationStatus.Suspended)
                {
                    part.Status = ParticipationStatus.Suspended;
                    await _context.SaveChangesAsync(token);
                }
            }
        }

        // 7. 返回
        string submissionType = triggerChallengeLock ? "Trap" : (bannedTrackId is not null ? "Ban" : "Wrong");

        return new ScoreResult
        {
            Status = AnswerResult.WrongAnswer,
            ScoreChange = -penaltyScore,
            IsPenalty = penaltyScore > 0,
            Level = level,
            TrackId = trackId,
            BannedTrackId = bannedTrackId,
            BannedLevel = bannedLevel,
            ChallengeLocked = triggerChallengeLock,
            UnlocksAt = unlocksAt,
            Message = submissionType == "Trap" ? "Trap answer triggered"
                : submissionType == "Ban" ? "Answer banned this track-level"
                : duplicate ? "Duplicate wrong submission, no penalty"
                : "Wrong answer"
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
    /// D7: Creates the entry when ANY single track's all levels have been solved.
    /// 一血播报：第一个完全解出任意 Track 的所有 Level 的 participation 触发。
    /// 对于无 Track 的传统单 flag 题目，只要有正确提交即创建 FirstSolve（保持向后兼容）。
    /// </summary>
    public async Task CreateFirstSolve(int participationId, int challengeId, int submissionId,
        CancellationToken token = default)
    {
        var exists = await _context.FirstSolves
            .AnyAsync(fs => fs.ParticipationId == participationId && fs.ChallengeId == challengeId, token);

        if (exists)
            return;

        var activeTrackIds = await _context.Set<ChallengeTrack>()
            .Where(t => t.ChallengeId == challengeId && t.IsEnabled)
            .Select(t => t.Id).ToListAsync(token);

        var hasBan = await _context.Set<ChallengeTrackLevelBanState>()
            .AnyAsync(b => b.ParticipationId == participationId
                && b.ChallengeId == challengeId
                && activeTrackIds.Contains(b.TrackId), token);
        if (hasBan) return;

        var levels = await _context.Set<ChallengeLevelConfig>()
            .Where(c => c.ChallengeId == challengeId && c.IsEnabled)
            .Select(c => c.Level).Distinct().ToListAsync(token);

        // 传统单 flag 题目（无 Track）：CreateFirstSolve 只在正确提交后调用，
        // 直接创建 FirstSolve（保持与原逻辑一致 — 空集视为 vacuously solved）
        if (activeTrackIds.Count == 0)
        {
            _context.FirstSolves.Add(new FirstSolve
            {
                ParticipationId = participationId,
                ChallengeId = challengeId,
                SubmissionId = submissionId
            });
            await _context.SaveChangesAsync(token);
            return;
        }

        // D7: 多 Track 题目 — 任意 Track 完全解出即创建 FirstSolve
        foreach (var trackId in activeTrackIds)
        {
            var allLevelsSolved = true;
            foreach (var level in levels)
            {
                var solved = await _context.SubmissionRecords.AnyAsync(r =>
                    r.ParticipationId == participationId
                    && r.ChallengeId == challengeId
                    && r.TrackId == trackId
                    && r.Level == level
                    && r.ScoreChange > 0, token);
                if (!solved)
                {
                    allLevelsSolved = false;
                    break;
                }
            }

            if (allLevelsSolved)
            {
                _context.FirstSolves.Add(new FirstSolve
                {
                    ParticipationId = participationId,
                    ChallengeId = challengeId,
                    SubmissionId = submissionId
                });
                await _context.SaveChangesAsync(token);
                return;
            }
        }
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
            ParticipationId = submission.ParticipationId,
            Level = result.Level,
            TrackId = result.TrackId,
            SubmissionType = result.Status == AnswerResult.Accepted ? "Correct"
                : result.ChallengeLocked ? "Trap"
                : result.BannedTrackId is not null ? "Ban"
                : result.Status == AnswerResult.FlagSubmitted ? "Duplicate"
                : "Wrong"
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
