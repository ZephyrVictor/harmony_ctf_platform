using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using CooldownInfo = GZCTF.Models.Request.Game.CooldownInfo;

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
    private readonly IChallengeCooldownRepository _cooldownRepo;

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
        IChallengeCooldownRepository cooldownRepo)
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
        _cooldownRepo = cooldownRepo;
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

        // 1. 过期锁清理（trap 锁过期后允许继续提交；冷却机制独立，不在此处重置）
        var expiredLocks = await _lockStateRepo.GetExpiredLocks(participation.Id, challenge.Id, token);
        if (expiredLocks.Count > 0)
        {
            await _lockStateRepo.DeleteLocks(expiredLocks, token);
        }

        // 2. 活跃锁检查（trap 锁）
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
                    Message = "该 Track 的当前 Level 已被封禁，请联系管理员"
                };
            }
        }

        // 2.6 题目冷却检查（per-challenge 答错次数 + 临时冷却）
        // 与 trap 锁相互独立；冷却中拒绝提交，到点自动解封
        var game = await _context.Games.AsNoTracking()
            .Where(g => g.Id == challenge.GameId)
            .Select(g => new { g.CooldownMinutes })
            .FirstOrDefaultAsync(token);
        var cooldownMinutes = game?.CooldownMinutes ?? 0;

        var tracks = await _trackRepo.GetTracks(challenge.Id, token);
        var trackCount = tracks.Length;
        var wrongAnswerLimit = trackCount * 5;

        var cooldownState = await _cooldownRepo.Get(participation.Id, challenge.Id, token);
        if (cooldownMinutes > 0 && cooldownState?.CooldownUntil is not null
            && cooldownState.CooldownUntil > DateTimeOffset.UtcNow)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                Message = $"题目冷却中，剩 {cooldownState.CooldownUntil.Value - DateTimeOffset.UtcNow:mm\\:ss}",
                Cooldown = new CooldownInfo
                {
                    WrongCount = cooldownState.WrongCount,
                    WrongAnswerLimit = wrongAnswerLimit,
                    CooldownMinutes = cooldownMinutes,
                    CooldownUntil = cooldownState.CooldownUntil
                }
            };
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

        var requestedLevelConfig = await _levelConfigRepo.GetConfig(challenge.Id, level, token);

        // 4. 找匹配的 flags
        var matchedFlags = candidates
            .Where(f => TryMatch(f, answer, requestedLevelConfig))
            .ToList();

        if (matchedFlags.Count == 0)
            return await HandleWrongSubmission(challenge, participation, answer, level, trackId,
                cooldownMinutes, wrongAnswerLimit, cooldownState, token);

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
                Message = "该级别已提交过，请继续提交下一 Level"
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

    private static bool TryMatch(ChallengeFlag flag, string answer, ChallengeLevelConfig? levelConfig = null)
    {
        if (flag.AnswerType == ChallengeFlagAnswerType.SourceLocation)
        {
            return TryMatchSourceLocation(flag, answer, levelConfig);
        }

        if (flag.AnswerType == ChallengeFlagAnswerType.Exact)
        {
            var comparison = levelConfig?.CaseSensitive == false
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(flag.FlagValue, answer, comparison);
        }

        var submitted = ParseHybridLineAnswer(answer, flag.AnswerType);
        if (submitted is null)
        {
            // 不能解析时尝试 Exact 字面比对
            return false;
        }

        var flagAnswerStr = string.IsNullOrWhiteSpace(flag.RawAnswer) ? flag.FlagValue : flag.RawAnswer;
        var flagAnswer = ParseHybridLineAnswer(flagAnswerStr, flag.AnswerType);
        if (flagAnswer is null)
        {
            return false;
        }

        return HybridLineMatches(flagAnswer.Value, submitted.Value);
    }

    public static bool TryMatchForTest(ChallengeFlag flag, string answer, ChallengeLevelConfig? levelConfig = null) =>
        TryMatch(flag, answer, levelConfig);

    private async Task<ScoreResult> HandleCorrectSubmission(
        GameChallenge challenge,
        Participation participation,
        ChallengeFlag matchedFlag,
        CancellationToken token)
    {
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, matchedFlag.Level, token);
        var score = levelConfig?.Score ?? 0;

        var submissionType = await DetermineSubmissionType(challenge, participation, token);

        // 答对时重置冷却计数（清零 WrongCount，清空 CooldownUntil）
        var cooldownState = await _cooldownRepo.Get(participation.Id, challenge.Id, token);
        if (cooldownState is not null && (cooldownState.WrongCount != 0 || cooldownState.CooldownUntil is not null))
        {
            cooldownState.WrongCount = 0;
            cooldownState.CooldownUntil = null;
            await _cooldownRepo.Upsert(cooldownState, token);
        }

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
        int cooldownMinutes,
        int wrongAnswerLimit,
        ChallengeCooldownState? cooldownState,
        CancellationToken token)
    {
        // 1. 加载 Level 配置（先加载以便检测 trap/ban）
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, level, token);

        var lockOnAnswers = levelConfig?.LockOnAnswers ?? [];
        var banOnAnswers = levelConfig?.BanOnAnswers ?? [];

        // 2. Trap / Ban 检测 — 必须在 dedup 之前，确保 ban 答案幂等生效
        bool triggerChallengeLock = false;
        string lockReason = "TrapAnswer";
        Guid? bannedTrackId = null;
        int? bannedLevel = null;

        if (lockOnAnswers.Contains(answer))
        {
            triggerChallengeLock = true;
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
                Cooldown = cooldownState is not null
                    ? new CooldownInfo
                    {
                        WrongCount = cooldownState.WrongCount,
                        WrongAnswerLimit = wrongAnswerLimit,
                        CooldownMinutes = cooldownMinutes,
                        CooldownUntil = cooldownState.CooldownUntil
                    }
                    : new CooldownInfo
                    {
                        WrongCount = 0,
                        WrongAnswerLimit = wrongAnswerLimit,
                        CooldownMinutes = cooldownMinutes,
                        CooldownUntil = null
                    },
                Message = "Duplicate wrong submission, no penalty"
            };
        }

        // 4. Penalty 计算（重复提交不扣分）
        var penaltyScore = 0;
        if (!duplicate)
        {
            if (levelConfig is null)
            {
                var penalty = await _penaltyRepo.GetPenalty(challenge.Id, token);
                var defaultPenalty = await _settings.Get<int>("Scoring.DefaultPenaltyScore", token);
                penaltyScore = CalculateWrongPenalty(levelConfig, penalty?.PenaltyScore, defaultPenalty);
            }
            else
            {
                penaltyScore = CalculateWrongPenalty(levelConfig, null, 0);
            }
        }

        // 5. 题目冷却计数（per-challenge，所有 track/level 合计）
        // 阈值 = trackCount × 5（自动）；冷却时长 = game.CooldownMinutes（per-game 配置）
        // 重复提交不累加；trap 触发时不进入冷却（trap 用 ChallengeLockState 处理）
        CooldownInfo? cooldownInfo = null;
        if (!duplicate && !triggerChallengeLock && cooldownMinutes > 0 && wrongAnswerLimit > 0)
        {
            var state = cooldownState ?? new ChallengeCooldownState
            {
                ParticipationId = participation.Id,
                ChallengeId = challenge.Id,
                WrongCount = 0,
                CooldownUntil = null
            };

            // 冷却已过期则清空 CooldownUntil（避免返回过期时间给前端）
            if (state.CooldownUntil is not null && state.CooldownUntil <= DateTimeOffset.UtcNow)
            {
                state.CooldownUntil = null;
            }

            state.WrongCount += 1;
            state.LastWrongAt = DateTimeOffset.UtcNow;

            if (state.WrongCount >= wrongAnswerLimit)
            {
                state.CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(cooldownMinutes);
                state.WrongCount = 0;
            }

            await _cooldownRepo.Upsert(state, token);

            cooldownInfo = new CooldownInfo
            {
                WrongCount = state.WrongCount,
                WrongAnswerLimit = wrongAnswerLimit,
                CooldownMinutes = cooldownMinutes,
                CooldownUntil = state.CooldownUntil
            };
        }

        // 6. 创建全题锁（仅 trap 触发；SubmissionLimit 已被冷却机制替代）
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
            Cooldown = cooldownInfo,
            Message = submissionType == "Trap" ? "Trap answer triggered"
                : submissionType == "Ban" ? "该 Track 的当前 Level 已被封禁，请联系管理员"
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

    private static int CalculateWrongPenalty(
        ChallengeLevelConfig? levelConfig,
        int? challengePenaltyScore,
        int defaultPenaltyScore)
        => Math.Max(0, levelConfig?.PenaltyScore ?? challengePenaltyScore ?? defaultPenaltyScore);

    public static int CalculateWrongPenaltyForTest(
        ChallengeLevelConfig? levelConfig,
        int? challengePenaltyScore,
        int defaultPenaltyScore)
        => CalculateWrongPenalty(levelConfig, challengePenaltyScore, defaultPenaltyScore);

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

    private static bool TryMatchSourceLocation(ChallengeFlag flag, string answer, ChallengeLevelConfig? levelConfig)
    {
        var flagAnswerStr = string.IsNullOrWhiteSpace(flag.RawAnswer) ? flag.FlagValue : flag.RawAnswer;
        var flagLocations = ParseSourceLocations(flagAnswerStr, levelConfig);
        var submittedLocations = ParseSourceLocations(answer, levelConfig);

        if (flagLocations.Count == 0 || submittedLocations.Count == 0)
            return false;

        var comparison = levelConfig?.PathCaseSensitive == false || levelConfig?.CaseSensitive == false
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (var submittedLocation in submittedLocations)
        {
            foreach (var flagLocation in flagLocations)
            {
                if (!string.Equals(flagLocation.Path, submittedLocation.Path, comparison))
                    continue;

                foreach (var submittedRange in submittedLocation.Ranges)
                {
                    foreach (var flagRange in flagLocation.Ranges)
                    {
                        if (submittedRange.Start >= flagRange.Start && submittedRange.End <= flagRange.End)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private static List<SourceLocationAnswer> ParseSourceLocations(string input, ChallengeLevelConfig? levelConfig)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        var trimmed = NormalizeRawSourceLocation(input);
        var orSeparators = string.IsNullOrEmpty(levelConfig?.OrSeparators)
            ? "、,;"
            : levelConfig.OrSeparators;
        var parts = trimmed.Split(orSeparators.ToCharArray(),
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length > 1 && parts.All(part => TrySplitSourceLocation(part, out _, out _)))
        {
            var locations = new List<SourceLocationAnswer>();
            foreach (var part in parts)
            {
                var location = ParseSourceLocation(part, levelConfig);
                if (location is null)
                    return [];
                locations.Add(location.Value);
            }

            return locations;
        }

        var single = ParseSourceLocation(trimmed, levelConfig);
        return single is null ? [] : [single.Value];
    }

    private static SourceLocationAnswer? ParseSourceLocation(string input, ChallengeLevelConfig? levelConfig)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = NormalizeRawSourceLocation(input);
        if (!TrySplitSourceLocation(trimmed, out var pathPart, out var lineExpr))
            return null;

        var path = NormalizeSourcePath(pathPart, levelConfig);
        if (levelConfig?.StripLinePrefix != false)
            lineExpr = StripLinePrefix(lineExpr).Trim();

        var ranges = ParseSourceLineRanges(lineExpr, levelConfig);
        return !string.IsNullOrWhiteSpace(path) && ranges.Count > 0
            ? new SourceLocationAnswer(path, ranges)
            : null;
    }

    private static bool TrySplitSourceLocation(string input, out string path, out string lineExpr)
    {
        path = string.Empty;
        lineExpr = string.Empty;

        var cPrefixIndex = input.LastIndexOf(":c:", StringComparison.OrdinalIgnoreCase);
        if (cPrefixIndex > 0 && cPrefixIndex + 3 < input.Length)
        {
            path = input[..cPrefixIndex];
            lineExpr = input[(cPrefixIndex + 1)..];
            return true;
        }

        var separatorIndex = input.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == input.Length - 1)
            return false;

        path = input[..separatorIndex];
        lineExpr = input[(separatorIndex + 1)..].Trim();
        return true;
    }

    private static List<LineRange> ParseSourceLineRanges(string input, ChallengeLevelConfig? levelConfig)
    {
        var orSeparators = string.IsNullOrEmpty(levelConfig?.OrSeparators)
            ? "、,;"
            : levelConfig.OrSeparators;
        var parts = input.Split(orSeparators.ToCharArray(),
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            parts = [input];

        List<LineRange> ranges = [];
        foreach (var part in parts)
        {
            var token = levelConfig?.StripLinePrefix == false ? part.Trim() : StripLinePrefix(part.Trim()).Trim();
            var rangeSeparator = string.IsNullOrWhiteSpace(levelConfig?.RangeSeparator)
                ? "-"
                : levelConfig.RangeSeparator;
            var rangeParts = token.Split(rangeSeparator, 2, StringSplitOptions.TrimEntries);

            if (rangeParts.Length == 1)
            {
                if (!int.TryParse(rangeParts[0], out var line) || line <= 0)
                    return [];
                ranges.Add(new LineRange(line, line));
                continue;
            }

            if (!int.TryParse(rangeParts[0], out var start)
                || !int.TryParse(rangeParts[1], out var end)
                || start <= 0
                || end < start)
            {
                return [];
            }

            ranges.Add(new LineRange(start, end));
        }

        return ranges;
    }

    private static string NormalizeSourcePath(string path, ChallengeLevelConfig? levelConfig)
    {
        var normalized = path.Trim()
            .Replace('\\', '/')
            .Replace('＼', '/')
            .Replace('∕', '/')
            .Replace('／', '/');
        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        return levelConfig?.PathCaseSensitive == false || levelConfig?.CaseSensitive == false
            ? normalized.ToUpperInvariant()
            : normalized;
    }

    private static string NormalizeRawSourceLocation(string input)
    {
        var normalized = input.Trim().Replace('：', ':');
        while (normalized.Contains("::", StringComparison.Ordinal))
            normalized = normalized.Replace("::", ":", StringComparison.Ordinal);
        return normalized;
    }

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

internal readonly record struct LineRange(int Start, int End);

internal readonly record struct SourceLocationAnswer(string Path, List<LineRange> Ranges);

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
