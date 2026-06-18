# 多级 Flag 题型迭代设计

**Date**: 2026-06-18
**Author**: ZephyrVictor
**Status**: Draft (待用户复核)
**Scope**: GameChallenge 答题模块重构（不含 ExerciseChallenge）

---

## 1. 背景与目标

### 1.1 比赛背景

第二届 OpenHarmony CTF 智能漏洞挖掘赛面向 AI 智能体，每个漏洞发现满分 10 分：
- 漏洞定位（文件路径 + 漏洞触发代码行 + 漏洞根因）4 分
- 漏洞类型（CWE）2 分
- 漏洞验证（PoC）4 分

防爆破规则：同一文件路径最多提交 5 份报告，超过视为爆破，暂停提交窗口。

### 1.2 现状缺口

现有 `GZCTF` 平台已添加 LineRange / HybridLine 匹配模式与 ChallengeFlag 的 OpenHarmony 硬编码字段，但存在以下缺口：

1. **无层级概念**：`ChallengeFlag.SortOrder` 仅做显示排序，不做 gating
2. **无 Track 概念**：同一题目下的多个漏洞实例无法分组
3. **无 per-level 配置**：提交次数、分值、penalty 只能在 Challenge 级
4. **无 trap 防爆破**：无法针对特定答案（如 "0"）触发锁定或封禁
5. **无批量导入**：`GameImportService` 是整场比赛迁移，非 flag 批量导入
6. **架构硬编码**：`FlagMatchMode` 枚举与 `Cwe/FilePath` 等字段绑死漏洞挖掘场景

### 1.3 设计目标

- **彻底重构**：不为向后兼容妥协，移除旧模式与硬编码字段
- **统一抽象**：用 Track + Level + Answer 三层模型表达任意多级题目
- **可配置可扩展**：per-level 配置分值/次数/trap/penalty，支持动态增删
- **智能体友好**：API 简洁完整，响应包含完整状态供 agent 决策
- **代码复用**：复用现有 `ParseHybridLineAnswer` / `HybridLineMatches` 等纯函数

---

## 2. 架构概览

### 2.1 核心抽象

```
Game (比赛)
  └─ GameChallenge (题目/模块，如 communication_netmanager_base)
       ├─ ChallengeTrack (漏洞实例 = 数据集一行)
       │    ├─ ChallengeFlag (Level 1 Answer，如 "services/.../netsys_sock_client.cpp:c:56")
       │    ├─ ChallengeFlag (Level 2 Answer，如 commit hash)
       │    └─ ChallengeFlag (Level 3 Answer，如 "CWE-476")
       └─ ChallengeLevelConfig (per-level 配置：Score/SubmissionLimit/PenaltyScore/LockOnAnswers/BanOnAnswers)
```

**层级关系**：
- `ChallengeFlag.TrackId` (Guid) → 同 TrackId 的 flags 属于同一漏洞实例（virtual FK 到 `ChallengeTrack.Id`）
- `ChallengeFlag.Level` (int) → 该 answer 属于第几级
- `ChallengeLevelConfig` (ChallengeId, Level) → 该级的统一配置，所有 Track 共享
- `ChallengeTrack` (ChallengeId, Id) → 漏洞实例元数据

### 2.2 统一匹配模型

- 移除 `Challenge.FlagMatchMode` 字段与 `FlagMatchMode` 枚举
- 所有匹配按 `ChallengeFlag.AnswerType` 分派（Exact/SingleLine/LineRange/MultiLine）
- 复用现有 `ScoringService.ParseHybridLineAnswer` / `HybridLineMatches` 纯函数

### 2.3 分层职责

| 层 | 职责 | 改动 |
|---|---|---|
| **Controllers** | HTTP 入口、参数校验、事务边界 | `ExtController` 扩展 Track/Level/Import 端点；`GameController.Submit` 接收 `(trackId, level)` |
| **Repositories** | 数据访问 | 新增 `ChallengeTrackRepository`、`ChallengeLevelConfigRepository`、`ChallengeLockStateRepository`、`ChallengeTrackLevelBanStateRepository`、`ChallengeWrongCountStateRepository` |
| **Services/Scoring** | 匹配、计分、锁定、解锁 | `ScoringService` 重构为统一 per-flag 分派；新增 `LockService`、`FlagImportService` |
| **Models** | 数据结构 | `ChallengeFlag` 加 `Level`+`TrackId`；新增 5 张表；移除 `Challenge.FlagMatchMode` 与 OpenHarmony 硬编码字段 |
| **Migrations** | DB schema | 单次不可逆迁移：新建 5 表 + 字段迁移 + 旧字段清理 |

---

## 3. 数据模型

### 3.1 `ChallengeFlag`（修订）

```csharp
public class ChallengeFlag {
    [Key] public Guid Id { get; set; }

    // 保留
    public string FlagValue { get; set; } = string.Empty;
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public string RawAnswer { get; set; } = string.Empty;
    public string NormalizedAnswerKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsOccupied { get; set; }  // dynamic attachment 用
    public int? AttachmentId { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // 新增
    public int Level { get; set; } = 1;
    public Guid TrackId { get; set; } = Guid.NewGuid();

    // 移除（迁移到 ChallengeTrack）
    // public string? FlawNo / Repository / Module / FilePath / Cwe

    // 移除（分值迁移到 ChallengeLevelConfig）
    // public int Score

    // 关系
    public int ChallengeId { get; set; }
    public GameChallenge? Challenge { get; set; }
    public int? ExerciseId { get; set; }  // 保留，exercise 模式暂不动
    public ExerciseChallenge? Exercise { get; set; }
    public Attachment? Attachment { get; set; }
    public List<SubmissionRecord> SubmissionRecords { get; set; } = [];
}
```

### 3.2 `ChallengeTrack`（新表）

```csharp
public class ChallengeTrack {
    [Key] public Guid Id { get; set; }  // = ChallengeFlag.TrackId
    public int ChallengeId { get; set; }

    public string? Title { get; set; }        // "Vulnerability #1" 或文件名
    public string? FlawNo { get; set; }       // 从 ChallengeFlag 迁移
    public string? Repository { get; set; }   // 从 ChallengeFlag 迁移
    public string? Module { get; set; }       // 从 ChallengeFlag 迁移
    public string? FilePath { get; set; }     // 从 ChallengeFlag 迁移
    public string? Category { get; set; }     // 新增：参考类别（如"跨界内存写"）
    public string? ReferenceUrl { get; set; } // 新增：社区公开 PR 链接

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public GameChallenge Challenge { get; set; } = null!;
}
```

### 3.3 `ChallengeLevelConfig`（新表）

```csharp
public class ChallengeLevelConfig {
    [Key] public int Id { get; set; }
    public int ChallengeId { get; set; }
    public int Level { get; set; }  // 1, 2, 3...

    public string? Title { get; set; }              // "漏洞定位" / "漏洞类型" / "漏洞验证"
    public int Score { get; set; }                  // 本 level 解出分值
    public int SubmissionLimit { get; set; } = 0;   // 0 = 继承 Challenge.SubmissionLimit
    public int PenaltyScore { get; set; } = 0;      // 0 = 继承 ChallengePenalty
    public List<string> LockOnAnswers { get; set; } = [];  // 触发临时全题锁
    public List<string> BanOnAnswers { get; set; } = [];   // 触发永久 (Track,Level) 封禁

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    public GameChallenge Challenge { get; set; } = null!;
}
```

### 3.4 `ChallengeLockState`（新表，临时全题锁）

```csharp
[Index(nameof(ParticipationId), nameof(ChallengeId))]
public class ChallengeLockState {
    [Key] public int Id { get; set; }
    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public int TriggeringLevel { get; set; }
    public Guid? TriggeringTrackId { get; set; }
    public string TriggerReason { get; set; }  // "TrapAnswer" | "SubmissionLimitExceeded"
    public string? TriggerAnswer { get; set; }
    public DateTimeOffset LockedAt { get; set; }
    public DateTimeOffset UnlocksAt { get; set; }
    public bool IsActive => DateTimeOffset.UtcNow < UnlocksAt;
}
```

### 3.5 `ChallengeTrackLevelBanState`（新表，永久封禁）

```csharp
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(TrackId), nameof(Level), IsUnique = true)]
public class ChallengeTrackLevelBanState {
    [Key] public int Id { get; set; }
    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public Guid TrackId { get; set; }
    public int Level { get; set; }
    public string BanAnswer { get; set; }
    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow;
    // 无 UnlocksAt —— 永久
}
```

### 3.6 `ChallengeWrongCountState`（新表，错误计数）

```csharp
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(Level), IsUnique = true)]
public class ChallengeWrongCountState {
    [Key] public int Id { get; set; }
    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public int Level { get; set; }  // per-level 计数
    public int WrongCount { get; set; }
    public DateTimeOffset LastResetAt { get; set; } = DateTimeOffset.UtcNow;
}
```

### 3.7 `Challenge` 修订

```csharp
public class Challenge {
    // 现有保留：Id, Title, Content, Category, Type, Hints, IsEnabled, DeadlineUtc,
    //           SubmissionLimit, ContainerImage, MemoryLimit, ..., FlagTemplate, etc.

    // 移除
    // public FlagMatchMode FlagMatchMode { get; set; }

    // 新增
    public int LockDurationMinutes { get; set; } = 0;  // 临时全题锁时长，0=永久

    // SubmissionLimit 保留为「整题默认」，ChallengeLevelConfig.SubmissionLimit=0 时回退到此
}
```

### 3.8 `SubmissionRecord` 修订

```csharp
public class SubmissionRecord {
    // 现有保留：Id, ScoreChange, IsPenalty, CreatedAt, SubmissionId, MatchedFlagId,
    //           GameId, ChallengeId, ParticipationId（denormalized）

    // 新增（便于审计与查询）
    public int? Level { get; set; }       // denormalized
    public Guid? TrackId { get; set; }    // denormalized
    public string? SubmissionType { get; set; }  // "Correct" | "Wrong" | "Trap" | "Ban" | "Duplicate"
}
```

### 3.9 索引策略

| 表 | 索引 |
|---|---|
| `ChallengeFlags` | `(ChallengeId, Level, TrackId)` 复合 |
| `ChallengeTracks` | `(ChallengeId, SortOrder)` |
| `ChallengeLevelConfigs` | `(ChallengeId, Level)` 唯一 |
| `ChallengeLockStates` | `(ParticipationId, ChallengeId, UnlocksAt)` |
| `ChallengeTrackLevelBanStates` | `(ParticipationId, ChallengeId, TrackId, Level)` 唯一 |
| `ChallengeWrongCountStates` | `(ParticipationId, ChallengeId, Level)` 唯一 |
| `SubmissionRecords` | `(SubmissionId)` 唯一（现有）；新增 `(ParticipationId, ChallengeId, TrackId, Level)` 复合 |

---

## 4. 匹配与计分流程

### 4.1 提交 API 请求体

```http
POST /api/game/{id}/challenges/{challengeId}
{
  "flag": "<encrypted answer>",
  "trackId": "guid" | null,    // null = Level 1 入口（自动匹配 Track）
  "level": 1                   // 1, 2, 3...
}
```

**语义**：
- `trackId = null, level = 1`：player 在 Level 1 输入框提交，系统在所有 Level 1 flags 中匹配，匹配成功后返回 `trackId`，前端记住上下文
- `trackId = "xxx", level = N (N≥2)`：player 在 Track xxx 的 Level N 输入框提交，系统只在该 (Track, Level) 内匹配

### 4.2 ScoringService.ProcessSubmission 流程

```
Submit(answer X, trackId T, level L) to Challenge C, Participation P:
  1. 过期锁清理：若 P 在 C 有过期锁 → 删除锁记录 + 重置所有 Level 的 WrongCount 为 0
  2. 全题锁检查：若 P 在 C 有活跃锁 → 返回 ChallengeLocked，带 UnlocksAt
  3. 匹配：
     a. T=null（Level 1 入口）：在 C 的所有 (any Track, Level=1) enabled flags 中匹配 X
     b. T 指定：在 C 的 (Track=T, Level=L) enabled flags 中匹配 X
  4. 匹配结果分类：
     - unlocked + unsolved + unbanned → 有效匹配（可计分）
     - already solved → 已解（ScoreChange=0）
     - banned → 已封禁（ScoreChange=0）
     - locked (Level>1 且 (T, L-1) 未解) → 锁定（ScoreChange=0，不扣分，不泄漏）
  5. 有有效匹配：
     a. 取 Level 最低（同 Level 取 SortOrder 最小）的 flag
     b. 返回 Accepted，ScoreChange = LevelConfig.Score
     c. 记录 SubmissionRecord（SubmissionType="Correct"）
     d. 触发 CreateFirstSolve 检查
  6. 无有效匹配：
     a. Duplicate 检查（同 answer 之前错误提交过）→ ScoreChange=0，SubmissionType="Duplicate"
     b. 进入 HandleWrongSubmission
```

### 4.3 HandleWrongSubmission 流程

```
HandleWrongSubmission(C, P, X, L, T?):
  1. Penalty 计算：
     LevelConfig.PenaltyScore > 0 ? 之 : ChallengePenalty.PenaltyScore
  2. Trap 检查（Level 2+ 才检查 BanOnAnswers；所有 Level 都检查 LockOnAnswers）：
     a. X ∈ LevelConfig.LockOnAnswers → 标记 triggerChallengeLock=true, reason="TrapAnswer"
     b. X ∈ LevelConfig.BanOnAnswers 且 T≠null（Level 2+）→ 创建 ChallengeTrackLevelBanState(T, L)
  3. WrongCount 累加：
     ChallengeWrongCountState(P, C, L).WrongCount++
  4. SubmissionLimit 检查：
     limit = LevelConfig.SubmissionLimit > 0 ? 之 : Challenge.SubmissionLimit
     若 limit > 0 且 WrongCount >= limit → triggerChallengeLock=true, reason="SubmissionLimitExceeded"
  5. 创建全题锁：
     若 triggerChallengeLock：
       UnlocksAt = LockDurationMinutes > 0 ? now+minutes : DateTime.MaxValue
       创建 ChallengeLockState
  6. 记录 SubmissionRecord：
     SubmissionType = triggerChallengeLock ? "Trap" : (BanOnAnswers 命中 ? "Ban" : "Wrong")
  7. 返回 ScoreResult：
     ScoreChange = -penalty
     ChallengeLocked, UnlocksAt, BannedTrackId, BannedLevel 等
```

### 4.4 FirstSolve 触发

```csharp
public async Task CreateFirstSolve(int participationId, int challengeId, int submissionId,
    CancellationToken token)
{
    if (await _context.FirstSolves.AnyAsync(
        fs => fs.ParticipationId == participationId && fs.ChallengeId == challengeId, token))
        return;

    // 只考虑启用中的 Track（软删除的 Track 不阻塞 FirstSolve）
    var activeTrackIds = await _context.Set<ChallengeTrack>()
        .Where(t => t.ChallengeId == challengeId && t.IsEnabled)
        .Select(t => t.Id).ToListAsync(token);

    // 若有活跃 Track 的 (Track, Level) 被封禁，永远无法 FirstSolve
    var hasBan = await _context.Set<ChallengeTrackLevelBanState>()
        .AnyAsync(b => b.ParticipationId == participationId
            && b.ChallengeId == challengeId
            && activeTrackIds.Contains(b.TrackId), token);
    if (hasBan) return;

    // 检查所有活跃 Track 的所有启用 Level 是否都解出
    var levels = await _context.Set<ChallengeLevelConfig>()
        .Where(c => c.ChallengeId == challengeId && c.IsEnabled)
        .Select(c => c.Level).Distinct().ToListAsync(token);

    foreach (var trackId in activeTrackIds)
    foreach (var level in levels)
    {
        var solved = await _context.SubmissionRecords.AnyAsync(r =>
            r.ParticipationId == participationId
            && r.ChallengeId == challengeId
            && r.TrackId == trackId
            && r.Level == level
            && r.ScoreChange > 0, token);
        if (!solved) return;
    }

    _context.FirstSolves.Add(new FirstSolve {
        ParticipationId = participationId,
        ChallengeId = challengeId,
        SubmissionId = submissionId
    });
    await _context.SaveChangesAsync(token);
}
```

**软删除 Track 的处理**：当 Track 被软删除（`IsEnabled=false`），其关联的 flags 也应标记 `IsEnabled=false`，封禁状态保留但不再阻塞 FirstSolve。管理员如需清理，可手动删除封禁记录（后续迭代提供管理工具）。

### 4.5 锁定与计数重置

**触发条件**：
- Trap answer 命中 LockOnAnswers
- WrongCount 达到 SubmissionLimit

**锁定期**：
- 全题锁（所有 Track、所有 Level 不能提交）
- 时长：`Challenge.LockDurationMinutes`（0 = 永久）

**到期后**：
- 下次提交时检测到锁过期 → 删除过期锁记录
- **重置所有 Level 的 WrongCount 为 0**（`LastResetAt = now`）
- player 可继续提交

**封禁（BanOnAnswers）**：
- 永久，不可恢复
- 仅影响 (Track, Level)，其他 Level 仍可提交
- 已解出的 (Track, Level) 不受影响（封禁无意义）
- Track 内有 Level 被封禁 → Track 永远无法 FirstSolve

### 4.6 ScoreResult 扩展

```csharp
public class ScoreResult {
    // 现有
    public AnswerResult Status { get; set; }
    public int ScoreChange { get; set; }
    public bool IsPenalty { get; set; }
    public Guid? MatchedFlagId { get; set; }
    public SubmissionType SubmissionType { get; set; }
    public string? Message { get; set; }

    // 新增
    public int? Level { get; set; }
    public Guid? TrackId { get; set; }
    public Guid? BannedTrackId { get; set; }
    public int? BannedLevel { get; set; }
    public bool ChallengeLocked { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
    public bool TrackCompleted { get; set; }
    public bool ChallengeFirstSolve { get; set; }
}
```

### 4.7 现有代码清理

| 旧代码 | 处理 |
|---|---|
| `Challenge.FlagMatchMode` 字段 | 移除 |
| `FlagMatchMode` 枚举（Exact/LineRange/HybridLine） | 移除 |
| `ScoringService.MatchLineRange` | 移除（被 `TryMatch` + `HybridLineMatches` 替代） |
| `ScoringService.MatchHybridLine` | 移除（被 `TryMatch` 替代） |
| `GameController.GetChallenge` 中 HybridLine 分支 | 统一为多级状态返回 |
| `ExtController.IsHybridLineChallenge` | 移除 |
| `ExtController.TrySetNormalizedAnswerKey` 的 HybridLine 分支 | 统一为 per-flag AnswerType |
| `SubmissionRecordRepository.HasCorrectSubmission` | 扩展支持 `(trackId, level)` 重载 |

---

## 5. 批量导入

### 5.1 导入流程

```
1. 管理员上传 xlsx/csv → POST /flags/import/preview
2. 后端解析列头 → 返回 detected columns + 样例行
3. 管理员在前端 UI 配置列映射（下拉框）→ 提交 mapping
4. POST /flags/import with file + mapping + mode
5. 后端按映射逐行 upsert ChallengeTrack + ChallengeFlag + ChallengeLevelConfig
6. 返回导入结果摘要（成功/失败/跳过 + 错误详情）
```

### 5.2 列映射模型

```csharp
public class FlagImportColumnMapping {
    // Track 元数据列（可空，不映射的字段不创建）
    public string? TrackTitleColumn { get; set; }
    public string? TrackFlawNoColumn { get; set; }
    public string? TrackRepositoryColumn { get; set; }
    public string? TrackModuleColumn { get; set; }
    public string? TrackFilePathColumn { get; set; }
    public string? TrackCategoryColumn { get; set; }
    public string? TrackReferenceUrlColumn { get; set; }

    // Track 匹配键（用于 upsert）
    public string MatchBy { get; set; } = "FilePath";  // FilePath | FlawNo | Title

    // Level 答案列（可配多个 Level）
    public List<LevelColumnMapping> LevelColumns { get; set; } = [];
}

public class LevelColumnMapping {
    public int Level { get; set; }                          // 1, 2, 3...
    public string ColumnName { get; set; } = string.Empty;  // e.g. "代码行数"
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public int Score { get; set; }                          // 该 Level 分值
    public int SubmissionLimit { get; set; }                // 0 = 继承
    public int PenaltyScore { get; set; }                   // 0 = 继承
    public List<string> LockOnAnswers { get; set; } = [];
    public List<string> BanOnAnswers { get; set; } = [];
    public string? LevelTitle { get; set; }
}
```

### 5.3 导入请求/响应

**预览**：
```http
POST /api/ext/challenges/{challengeId}/flags/import/preview
Content-Type: multipart/form-data

file: <xlsx/csv file>

→ {
  columnNames: ["所属仓库", "文件路径", "代码行数", ...],
  sampleRows: [
    { "所属仓库": "communication_netmanager_base", "文件路径": "services/...", ... },
    ...
  ]
}
```

**导入**：
```http
POST /api/ext/challenges/{challengeId}/flags/import
Content-Type: multipart/form-data

file: <xlsx/csv file>
mapping: <JSON FlagImportColumnMapping>
mode: "upsert" | "skip" | "fail"

→ {
  totalRows: 60,
  imported: 55,
  updated: 3,
  skipped: 2,
  failed: 0,
  errors: [
    { row: 12, error: "Level 1 answer is empty" },
    { row: 47, error: "Invalid line range format: 'c:abc'" }
  ]
}
```

### 5.4 Upsert 逻辑

```
对每一行 data:
  1. 解析 Track 元数据（按 mapping 中的 TrackXxxColumn 从 data 取值）
  2. 查找现有 ChallengeTrack：
     - 按 MatchBy 字段匹配（FilePath | FlawNo | Title）
     - 匹配键为空 → 跳过该行，记录错误
  3. 存在 → 更新 Track 元数据 + 删除旧 flags（无提交记录时）+ 创建新 flags
     存在且有提交记录 → 标记 IsEnabled=false，新建 Track
     不存在 → 创建新 Track + 创建 flags
  4. 为每个 Level 创建/更新 ChallengeLevelConfig（按 Level 编号 upsert）
  5. 单行失败 → 记录到 errors，继续下一行（不回滚整个导入）
  6. 同一 Track 的 flag 创建是事务性的（要么全部成功，要么该 Track 回滚）
```

**冲突模式**：
- `upsert`：存在则更新，不存在则新建
- `skip`：存在则跳过，仅新建
- `fail`：存在则整个导入失败回滚

### 5.5 文件解析

- **xlsx**：复用 `NPOI`（项目已有依赖）
- **csv**：用 `CsvHelper` 或手写解析
- 列头检测：第一行作为列头

### 5.6 FlagImportService

```csharp
public class FlagImportService {
    public Task<FlagImportFilePreview> PreviewFile(
        Stream fileStream, string contentType, CancellationToken token);

    public Task<FlagImportResult> ImportFlags(
        int challengeId,
        Stream fileStream,
        string contentType,
        FlagImportColumnMapping mapping,
        ImportMode mode,
        CancellationToken token);
}

public class FlagImportFilePreview {
    public List<string> ColumnNames { get; set; } = [];
    public List<Dictionary<string, string>> SampleRows { get; set; } = [];
}

public class FlagImportResult {
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<FlagImportError> Errors { get; set; } = [];
}
```

### 5.7 复用现有

- `IChallengeFlagRepository.CreateFlag / UpdateFlag / DeleteFlag` — 复用
- `IGameChallengeRepository.LoadFlags` — 复用
- 事务：复用 `IRepositoryBase.BeginTransactionAsync`
- xlsx 解析：复用 `NPOI`

---

## 6. API 端点

### 6.1 管理员 API（`/api/ext`）

#### Track 管理
```http
GET    /api/ext/challenges/{challengeId}/tracks
POST   /api/ext/challenges/{challengeId}/tracks
GET    /api/ext/challenges/{challengeId}/tracks/{trackId:guid}
PUT    /api/ext/challenges/{challengeId}/tracks/{trackId:guid}
DELETE /api/ext/challenges/{challengeId}/tracks/{trackId:guid}   # 软删除（IsEnabled=false）
```

#### Flag 管理（现有，扩展请求体）
```http
GET    /api/ext/challenges/{challengeId}/flags
POST   /api/ext/challenges/{challengeId}/flags
       body: { flagValue, answerType, rawAnswer?, level, trackId?, isVisible?, isEnabled?, sortOrder? }
       # trackId 可空，空时自动创建新 Track
PUT    /api/ext/challenges/{challengeId}/flags/{flagId:guid}
DELETE /api/ext/challenges/{challengeId}/flags/{flagId:guid}   # 有提交记录则 409
```

#### Level 配置管理
```http
GET    /api/ext/challenges/{challengeId}/levels
POST   /api/ext/challenges/{challengeId}/levels
       body: { level, title?, score, submissionLimit?, penaltyScore?, lockOnAnswers?, banOnAnswers?, isEnabled?, sortOrder? }
PUT    /api/ext/challenges/{challengeId}/levels/{levelId:int}
DELETE /api/ext/challenges/{challengeId}/levels/{levelId:int}   # 若该 level 已有 flags 则 409
```

#### 批量导入
```http
POST   /api/ext/challenges/{challengeId}/flags/import/preview
POST   /api/ext/challenges/{challengeId}/flags/import
```

#### Challenge 配置扩展（EditController）
```http
PUT    /api/edit/games/{id}/challenges/{cId}
       body: 现有字段 + lockDurationMinutes?
```

### 6.2 选手 / 智能体 API（`/api/game`）

#### Challenge 详情（扩展响应）
```http
GET    /api/game/{id}/challenges/{challengeId}

→ {
  // 现有字段
  id, title, content, hints, ...

  // 多级状态（新增）
  tracks: [
    {
      trackId: "guid",
      title: "Vulnerability #1",
      isEnabled: true,
      levels: [
        {
          level: 1,
          title: "漏洞定位",
          status: "Solved",        // Unsolved | Solved | Banned | Locked
          solvedAt: "2026-06-18T...",
          attemptsUsed: 2,
          submissionLimit: 5,
          unlocksAt: null
        },
        {
          level: 2,
          title: "漏洞类型",
          status: "Unsolved",
          solvedAt: null,
          attemptsUsed: 0,
          submissionLimit: 5,
          unlocksAt: null
        },
        { level: 3, status: "Locked", ... }
      ]
    },
    { trackId: "guid2", ... }
  ],

  challengeLock: {
    isLocked: false,
    unlocksAt: null,
    reason: null
  },

  currentLevel: 1
}
```

#### 提交（扩展请求体）
```http
POST   /api/game/{id}/challenges/{challengeId}
       body: {
         flag: "<encrypted>",
         trackId: "guid" | null,
         level: 1
       }

→ {
  status: "Accepted" | "WrongAnswer" | "FlagSubmitted",
  scoreChange: 4,
  totalScore: 12,
  matchedFlagId: "guid" | null,
  trackId: "guid" | null,    // Level 1 入口匹配成功后返回
  level: 1,
  message: "...",
  trackLevelBanned: false,
  bannedTrackId: null,
  bannedLevel: null,
  challengeLocked: false,
  unlocksAt: null,
  trackCompleted: false,
  challengeFirstSolve: false
}
```

#### 提交记录查询
```http
GET    /api/game/{id}/challenges/{challengeId}/submissions?trackId=guid&level=1
```

### 6.3 响应状态码约定

| 场景 | HTTP | Response.Status |
|---|---|---|
| 正确提交（首次） | 200 | Accepted |
| 正确提交（重复，已解） | 200 | FlagSubmitted (ScoreChange=0) |
| 错误提交（普通） | 200 | WrongAnswer (ScoreChange=-penalty) |
| 错误提交（Trap，触发封禁） | 200 | WrongAnswer (BannedTrackId/Level 填充) |
| 错误提交（Trap，触发全题锁） | 200 | WrongAnswer (ChallengeLocked=true) |
| 错误提交（超 SubmissionLimit，触发全题锁） | 200 | WrongAnswer (ChallengeLocked=true) |
| 全题锁中提交 | 400 | Challenge locked until {UnlocksAt} |
| Level 未解锁 | 400 | Previous level not solved |
| (Track, Level) 已封禁 | 200 | FlagSubmitted (ScoreChange=0) |

### 6.4 智能体友好性

- 所有响应包含 `message` 字段（人类可读）
- 错误响应包含 `errorCode` 和 `details`（机器可读）
- `GET /api/game/{id}/challenges/{challengeId}` 返回完整状态，agent 可自主决策下一步
- 提交响应包含 `trackId`（Level 1 入口匹配后），agent 可记忆上下文
- 认证：复用现有 `ApiToken` 机制

### 6.5 SignalR 通知（BigScreenHub 扩展）

- `TrackLevelSolved` (participationId, challengeId, trackId, level, score)
- `TrackLevelBanned` (participationId, challengeId, trackId, level, banAnswer)
- `ChallengeLocked` (participationId, challengeId, unlocksAt, reason)
- `TrackCompleted` (participationId, challengeId, trackId)
- `ChallengeFirstSolve` (participationId, challengeId)

---

## 7. 迁移策略

### 7.1 EF Core 迁移（单次，不可逆）

**迁移文件**：`20260620_AddMultiLevelFlagModel.cs`

**步骤**：
1. 创建 5 张新表：`ChallengeTracks`, `ChallengeLevelConfigs`, `ChallengeLockStates`, `ChallengeTrackLevelBanStates`, `ChallengeWrongCountStates`
2. `GameChallenges` 加 `LockDurationMinutes` 列（default 0）
3. `ChallengeFlags` 加 `Level` (default 1) + `TrackId` (default new Guid) 列
4. `SubmissionRecords` 加 `Level` + `TrackId` + `SubmissionType` 可空列
5. 数据迁移 SQL：
   - 为每个 `ChallengeFlag` 创建 `ChallengeTrack`（复制 FlawNo/Repository/Module/FilePath）
   - 设置 `ChallengeFlag.TrackId` = 新建 `ChallengeTrack.Id`
   - 为每个 `(ChallengeId, Level=1)` 创建 `ChallengeLevelConfig`（Score 从 ChallengeFlag.Score 迁移）
   - `SubmissionRecords` 回填 `Level=1`, `TrackId` = 对应 flag 的 TrackId
6. 删除旧列：
   - `ChallengeFlags`: `FlawNo`, `Repository`, `Module`, `FilePath`, `Cwe`, `Score`
   - `GameChallenges`: `FlagMatchMode`
7. 创建索引（见 §3.9）

### 7.2 数据迁移 SQL（关键部分）

**注意**：用户已确认「每个旧 flag 独立成一个 track」（旧题没有 track 概念）。迁移需保证 flag-to-track 一一对应，避免多个 flag 抢同一个 track。

```sql
-- a. 为每个 ChallengeFlag 创建独立 ChallengeTrack，用临时列 _SourceFlagId 记录来源
ALTER TABLE "ChallengeTracks" ADD COLUMN "_SourceFlagId" uuid;

INSERT INTO "ChallengeTracks" ("Id", "ChallengeId", "Title", "FlawNo", "Repository", "Module", "FilePath", "IsEnabled", "SortOrder", "CreatedAt", "UpdatedAt", "_SourceFlagId")
SELECT
    gen_random_uuid(),
    cf."ChallengeId",
    COALESCE(cf."FilePath", cf."FlawNo", 'Track ' || cf."Id"::text),
    cf."FlawNo",
    cf."Repository",
    cf."Module",
    cf."FilePath",
    true,
    cf."SortOrder",
    NOW(),
    NOW(),
    cf."Id"  -- 记录来源 flag ID
FROM "ChallengeFlags" cf;

-- b. 回填 ChallengeFlag.TrackId（通过 _SourceFlagId 精确匹配，保证 1:1）
UPDATE "ChallengeFlags" cf
SET "TrackId" = ct."Id"
FROM "ChallengeTracks" ct
WHERE ct."_SourceFlagId" = cf."Id";

-- c. 删除临时列
ALTER TABLE "ChallengeTracks" DROP COLUMN "_SourceFlagId";

-- d. 为每个 (ChallengeId, Level=1) 创建 ChallengeLevelConfig
INSERT INTO "ChallengeLevelConfigs" ("ChallengeId", "Level", "Title", "Score", "SubmissionLimit", "PenaltyScore", "IsEnabled", "SortOrder")
SELECT cf."ChallengeId", 1, 'Level 1', MAX(cf."Score"), 0, 0, true, 1
FROM "ChallengeFlags" cf
GROUP BY cf."ChallengeId";

-- e. SubmissionRecords 回填
UPDATE "SubmissionRecords" sr
SET "Level" = 1, "TrackId" = cf."TrackId", "SubmissionType" = 'Correct'
FROM "ChallengeFlags" cf
WHERE sr."MatchedFlagId" = cf."Id";
```

### 7.3 代码删除清单

| 文件 | 删除内容 |
|---|---|
| `Utils/Enums.cs` | `FlagMatchMode` 枚举（保留 `ChallengeFlagAnswerType`） |
| `Models/Data/Challenge.cs` | `FlagMatchMode` 字段 |
| `Models/Data/GameChallenge.cs` | `Update()` 方法中 `FlagMatchMode` 赋值 |
| `Models/Request/Edit/ChallengeUpdateModel.cs` | `FlagMatchMode` 字段 |
| `Models/Request/Edit/ChallengeEditDetailModel.cs` | `FlagMatchMode` 字段及映射 |
| `Models/Request/Game/ChallengeDetailModel.cs` | `FlagMatchMode` 字段 |
| `Services/Scoring/ScoringService.cs` | `MatchLineRange`、`MatchHybridLine`（保留 `ParseHybridLineAnswer`、`HybridLineMatches` 等纯函数） |
| `Controllers/ExtController.cs` | `IsHybridLineChallenge`、HybridLine 专用逻辑 |
| `Controllers/GameController.cs` | `GetChallenge` 中 HybridLine 分支 |
| `Migrations/20260515110000_AddHybridLineFlagSpec.cs` 等 | 不删，历史迁移保留 |

### 7.4 GameImportService 同步更新

- `TransferExtensions.ToTransfer` / `FromTransfer` 扩展：序列化 `ChallengeTrack[]` + `ChallengeLevelConfig[]` + `Challenge.LockDurationMinutes`
- `TransferChallenge` 模型加 `Tracks: TransferTrack[]` 和 `Levels: TransferLevelConfig[]`
- 导入时按 trackId upsert（保留原 ID 或重新生成）

---

## 8. 前端交互

### 8.1 页面结构

```
/admin/games/{id}/challenges/{cId}    Challenge 编辑页（扩展）
  ├─ 基础信息（现有）
  ├─ Flag 配置（重构）
  │    ├─ Levels 配置 tab
  │    │    └─ Level 列表（CRUD，可拖拽排序）
  │    ├─ Tracks 配置 tab
  │    │    └─ Track 列表（CRUD，展开显示该 Track 的 flags）
  │    ├─ 批量导入 tab
  │    │    ├─ 文件上传 + 列映射 UI
  │    │    └─ 导入结果展示
  │    └─ 防爆破配置 tab
  │         └─ LockDurationMinutes + Trap answers 配置
  └─ 容器配置（现有）

/games/{id}/challenges/{cId}    选手 Challenge 详情页（重构）
  ├─ Challenge 描述（现有）
  ├─ Track 列表
  │    └─ 每个 Track 显示进度（Level 1 ✓ / Level 2 进行中 / Level 3 锁定）
  └─ Track 详情弹窗（点击 Track 进入）
       ├─ Level 1 输入框（若未解）
       ├─ Level 2 输入框（Level 1 解出后显示）
       ├─ Level 3 输入框（Level 2 解出后显示）
       └─ 每个 Level 显示：状态、剩余次数、解锁时间（若锁定）
```

### 8.2 前端关键组件

- `TrackList.tsx` — Track 列表 + 进度展示
- `TrackDetailModal.tsx` — 单 Track 多 Level 提交弹窗
- `LevelConfigEditor.tsx` — 管理员 Level 配置编辑器
- `FlagImportDialog.tsx` — 批量导入对话框（文件上传 → 列映射 → 预览 → 导入）
- `ColumnMappingForm.tsx` — 列映射表单

### 8.3 API 客户端（`Api.ts`）扩展

- `getChallengeTracks(challengeId)`
- `createTrack / updateTrack / deleteTrack`
- `getLevelConfigs / createLevelConfig / updateLevelConfig / deleteLevelConfig`
- `previewFlagImport(challengeId, file)`
- `importFlags(challengeId, file, mapping, mode)`
- `submitFlag(gameId, challengeId, { flag, trackId, level })` — 扩展现有

### 8.4 国际化

- `admin.json`: track/level/import 相关翻译
- `challenge.json`: player 视图多级状态翻译

---

## 9. 测试策略

### 9.1 单元测试（`GZCTF.Test`）

- `ScoringServiceTests`:
  - 正确提交（首次/重复/已解/已封禁）
  - 错误提交（普通/trap/超限）
  - 解锁检查（Level 1 入口/Level 2+ 指定 Track）
  - Trap 触发（LockOnAnswers/BanOnAnswers/同时）
  - 全题锁到期 + WrongCount 重置
  - FirstSolve 触发（所有 Track 所有 Level 解出）
  - FirstSolve 不可达（有封禁）
- `FlagImportServiceTests`:
  - 文件解析（xlsx/csv）
  - 列映射（正确/缺失/多余）
  - Upsert（新建/更新/冲突）
  - 错误处理（单行失败不阻塞）
- `MatchPureFunctionsTests`:
  - `TryMatch` 各 AnswerType 分派
  - `ParseHybridLineAnswer` / `HybridLineMatches`（现有，保留）

### 9.2 集成测试（`GZCTF.Integration.Test`）

- `MultiLevelFlagFlowTests`:
  - 完整流程：导入 → 创建题目 → 选手提交 Level 1 → Level 2 → Level 3 → Track 完成 → FirstSolve
  - Trap 触发：提交 "0" → 封禁 → 其他 Level 仍可解 → Track 无法 FirstSolve
  - 超限：5 次错误 → 全题锁 → 到期 → 重置 → 继续提交
- `FlagImportTests`:
  - 上传 OH 数据集 xlsx → 正确解析 60 行 → 创建 60 个 Track × 3 Level = 180 个 flag

### 9.3 手动验证清单

- 用 OH 数据集（60 行）导入到 communication_netmanager_base 等题目
- 模拟 player 提交 file:line → commit → CWE 完整流程
- 触发 trap answer 验证封禁
- 触发超限验证全题锁
- 验证 FirstSolve 在所有 Track 完成后触发

---

## 10. 未覆盖事项

以下不在本次迭代范围：

- **ExerciseChallenge**：练习题模式不改造，保持现状
- **动态容器题目**：DynamicContainer 类型的题目不强制使用 Track/Level 模型（但可选用）
- **管理员 UI 拖拽排序**：Level/Track 排序仅通过 SortOrder 字段 API 暴露，前端拖拽实现由前端任务负责
- **导入回滚**：导入失败不回滚已成功行，仅返回错误报告；管理员可通过 Upsert 重新导入修正

---

## 11. 风险与权衡

| 风险 | 缓解 |
|---|---|
| 迁移不可逆，旧数据可能丢失 | 迁移前要求 DB 备份；数据迁移 SQL 在测试库验证 |
| 统一匹配模型破坏现有 LineRange/HybridLine 题目 | 用户明确不需要向后兼容；迁移脚本自动转换 |
| 批量导入单行失败影响其他行 | 采用 partial success 模式，返回详细错误报告 |
| Trap 答案配置错误导致玩家被误封 | BanOnAnswers 为 per-level 配置，管理员可精确控制；封禁状态可由管理员手动清除（后续迭代） |
| 智能体可能通过尝试探测答案 | 锁定状态返回与已解相同的「不计分」响应，不泄漏答案正确性 |
| 锁到期后 WrongCount 重置可被循环利用 | LockDurationMinutes 可配置足够长（如 30 分钟），比赛时长有限 |

---

## 12. 实现顺序建议

1. **数据模型与迁移**：新增实体 + EF Core 迁移 + 数据回填
2. **Repositories**：5 个新 Repository 接口与实现
3. **ScoringService 重构**：统一匹配 + Trap/Lock/Ban 逻辑
4. **Controllers**：ExtController 扩展 + GameController 提交扩展
5. **FlagImportService**：文件解析 + 列映射 + Upsert
6. **GameImportService 同步**：Transfer 模型扩展
7. **前端 API 客户端 + 页面**：管理端 + 选手端
8. **测试**：单元 + 集成
9. **清理**：删除旧代码（FlagMatchMode 枚举、MatchLineRange 等）
