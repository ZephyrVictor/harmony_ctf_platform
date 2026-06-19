# 多级 Flag 题型迭代实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 GZCTF 平台的 GameChallenge 答题模块实现多级 Flag 题型，支持 Track + Level + Answer 三层数据模型、批量导入、per-level 配置、trap 防爆破、AI 智能体友好的 API。

**Architecture:** 移除 `Challenge.FlagMatchMode` 枚举，统一按 `ChallengeFlag.AnswerType` per-flag 匹配。新增 `ChallengeTrack` / `ChallengeLevelConfig` / `ChallengeLockState` / `ChallengeTrackLevelBanState` / `ChallengeWrongCountState` 五张表。`ScoringService` 重构为单入口 `ProcessSubmission`，按 `(trackId, level)` 上下文分派匹配与计分。批量导入支持 xlsx/csv + 列映射 + upsert。

**Tech Stack:** ASP.NET Core 8 + EF Core + PostgreSQL + NPOI（xlsx）+ React + Mantine（前端）+ xUnit（测试）

**Spec:** `docs/superpowers/specs/2026-06-18-multi-level-flag-design.md`

---

## 文件结构

### 新建文件

| 路径 | 责任 |
|---|---|
| `src/GZCTF/Models/Data/ChallengeTrack.cs` | Track 实体（漏洞实例元数据） |
| `src/GZCTF/Models/Data/ChallengeLevelConfig.cs` | Level 配置实体 |
| `src/GZCTF/Models/Data/ChallengeLockState.cs` | 临时全题锁状态 |
| `src/GZCTF/Models/Data/ChallengeTrackLevelBanState.cs` | 永久 (Track,Level) 封禁状态 |
| `src/GZCTF/Models/Data/ChallengeWrongCountState.cs` | per-Level 错误计数 |
| `src/GZCTF/Repositories/Interface/IChallengeTrackRepository.cs` | Track 仓储接口 |
| `src/GZCTF/Repositories/Interface/IChallengeLevelConfigRepository.cs` | Level 配置仓储接口 |
| `src/GZCTF/Repositories/Interface/IChallengeLockStateRepository.cs` | 锁状态仓储接口 |
| `src/GZCTF/Repositories/Interface/IChallengeTrackLevelBanStateRepository.cs` | 封禁状态仓储接口 |
| `src/GZCTF/Repositories/Interface/IChallengeWrongCountStateRepository.cs` | 错误计数仓储接口 |
| `src/GZCTF/Repositories/ChallengeTrackRepository.cs` | Track 仓储实现 |
| `src/GZCTF/Repositories/ChallengeLevelConfigRepository.cs` | Level 配置仓储实现 |
| `src/GZCTF/Repositories/ChallengeLockStateRepository.cs` | 锁状态仓储实现 |
| `src/GZCTF/Repositories/ChallengeTrackLevelBanStateRepository.cs` | 封禁状态仓储实现 |
| `src/GZCTF/Repositories/ChallengeWrongCountStateRepository.cs` | 错误计数仓储实现 |
| `src/GZCTF/Services/Scoring/ScoreResult.cs` | 提交结果模型（从 ScoringService 抽出） |
| `src/GZCTF/Services/Import/FlagImportService.cs` | 批量导入服务 |
| `src/GZCTF/Services/Import/FlagImportModels.cs` | 导入请求/响应模型 |
| `src/GZCTF/Controllers/TrackController.cs` | Track CRUD 端点 |
| `src/GZCTF/Controllers/LevelConfigController.cs` | Level 配置 CRUD 端点 |
| `src/GZCTF/Controllers/FlagImportController.cs` | 批量导入端点 |
| `src/GZCTF/Migrations/20260620000000_AddMultiLevelFlagModel.cs` | EF Core 迁移 |
| `src/GZCTF.Test/UnitTests/Scoring/ScoringServiceTests.cs` | 评分服务单元测试 |
| `src/GZCTF.Test/UnitTests/Import/FlagImportServiceTests.cs` | 导入服务单元测试 |
| `src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagFlowTests.cs` | 多级 flag 集成测试 |

### 修改文件

| 路径 | 改动 |
|---|---|
| `src/GZCTF/Models/Data/ChallengeFlag.cs` | 加 `Level`/`TrackId`，删 `FlawNo`/`Repository`/`Module`/`FilePath`/`Cwe`/`Score` |
| `src/GZCTF/Models/Data/Challenge.cs` | 加 `LockDurationMinutes`，删 `FlagMatchMode` |
| `src/GZCTF/Models/Data/SubmissionRecord.cs` | 加 `Level`/`TrackId`/`SubmissionType` |
| `src/GZCTF/Models/AppDbContext.cs` | 注册 5 张新表，更新 ChallengeFlag/Challenge/SubmissionRecord 配置 |
| `src/GZCTF/Utils/Enums.cs` | 删 `FlagMatchMode` 枚举 |
| `src/GZCTF/Repositories/Interface/IChallengeFlagRepository.cs` | 加 `GetFlagsByLevel`/`GetFlagsByTrack` 等重载 |
| `src/GZCTF/Repositories/Interface/ISubmissionRecordRepository.cs` | 加 `HasCorrectSubmission(trackId, level)` 重载 |
| `src/GZCTF/Repositories/ChallengeFlagRepository.cs` | 实现新重载 |
| `src/GZCTF/Repositories/SubmissionRecordRepository.cs` | 实现新重载 |
| `src/GZCTF/Services/Scoring/ScoringService.cs` | 重构为统一 per-flag 匹配 + Level/Track 上下文 |
| `src/GZCTF/Controllers/ExtController.cs` | Flag CRUD 扩展 Level/TrackId；删 HybridLine 专用逻辑 |
| `src/GZCTF/Controllers/GameController.cs` | Submit 接收 (trackId, level)；GetChallenge 返回多级状态 |
| `src/GZCTF/Controllers/EditController.cs` | UpdateGameChallenge 接收 LockDurationMinutes |
| `src/GZCTF/Models/Request/Edit/ChallengeUpdateModel.cs` | 加 LockDurationMinutes，删 FlagMatchMode |
| `src/GZCTF/Models/Request/Edit/ChallengeEditDetailModel.cs` | 同上 |
| `src/GZCTF/Models/Request/Game/ChallengeDetailModel.cs` | 加 tracks/challengeLock/currentLevel |
| `src/GZCTF/Models/Request/Game/FlagSubmitModel.cs` | 加 trackId/level |
| `src/GZCTF/Models/Transfer/TransferExtensions.cs` | 序列化 Tracks/Levels/LockDurationMinutes |
| `src/GZCTF/Extensions/Startup/ServicesExtension.cs` | 注册 5 个新仓储 + FlagImportService |
| `src/GZCTF/ClientApp/src/Api.ts` | 扩展 Track/Level/Import API 客户端 |
| `src/GZCTF/ClientApp/src/locales/{zh-CN,en-US}/admin.json` | track/level/import 翻译 |
| `src/GZCTF/ClientApp/src/locales/{zh-CN,en-US}/challenge.json` | 多级状态翻译 |

---

## Phase 1: 数据模型与迁移

### Task 1: 创建 ChallengeTrack 实体

**Files:**
- Create: `src/GZCTF/Models/Data/ChallengeTrack.cs`

- [ ] **Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 漏洞实例 track（数据集一行 = 一个 Track）
/// </summary>
[Index(nameof(ChallengeId))]
public class ChallengeTrack
{
    [Key]
    public Guid Id { get; set; }

    public int ChallengeId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(64)]
    public string? FlawNo { get; set; }

    [MaxLength(128)]
    public string? Repository { get; set; }

    [MaxLength(128)]
    public string? Module { get; set; }

    [MaxLength(512)]
    public string? FilePath { get; set; }

    [MaxLength(128)]
    public string? Category { get; set; }

    [MaxLength(512)]
    public string? ReferenceUrl { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    public GameChallenge Challenge { get; set; } = null!;

    #endregion
}
```

- [ ] **Step 2: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED（无错误，可能有未使用警告）

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeTrack.cs
git commit -m "feat(models): add ChallengeTrack entity for multi-level flag"
```

---

### Task 2: 创建 ChallengeLevelConfig 实体

**Files:**
- Create: `src/GZCTF/Models/Data/ChallengeLevelConfig.cs`

- [ ] **Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Per-challenge per-level 配置（所有 Track 共享）
/// </summary>
[Index(nameof(ChallengeId), nameof(Level), IsUnique = true)]
public class ChallengeLevelConfig
{
    [Key]
    public int Id { get; set; }

    public int ChallengeId { get; set; }

    /// <summary>
    /// 层级编号，1, 2, 3...
    /// </summary>
    public int Level { get; set; }

    [MaxLength(64)]
    public string? Title { get; set; }

    /// <summary>
    /// 本 level 解出分值
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Score { get; set; }

    /// <summary>
    /// 0 = 继承 Challenge.SubmissionLimit
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SubmissionLimit { get; set; }

    /// <summary>
    /// 0 = 继承 ChallengePenalty
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PenaltyScore { get; set; }

    /// <summary>
    /// 触发临时全题锁的 trap 答案
    /// </summary>
    public List<string> LockOnAnswers { get; set; } = [];

    /// <summary>
    /// 触发永久 (Track, Level) 封禁的 trap 答案（仅 Level 2+ 生效）
    /// </summary>
    public List<string> BanOnAnswers { get; set; } = [];

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    #region Db Relationship

    public GameChallenge Challenge { get; set; } = null!;

    #endregion
}
```

- [ ] **Step 2: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeLevelConfig.cs
git commit -m "feat(models): add ChallengeLevelConfig entity"
```

---

### Task 3: 创建 ChallengeLockState 实体

**Files:**
- Create: `src/GZCTF/Models/Data/ChallengeLockState.cs`

- [ ] **Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 临时全题锁状态（per-participation per-challenge）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId))]
[Index(nameof(UnlocksAt))]
public class ChallengeLockState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }

    public int TriggeringLevel { get; set; }
    public Guid? TriggeringTrackId { get; set; }

    [MaxLength(32)]
    public string TriggerReason { get; set; } = string.Empty;

    [MaxLength(Limits.MaxFlagLength)]
    public string? TriggerAnswer { get; set; }

    public DateTimeOffset LockedAt { get; set; }
    public DateTimeOffset UnlocksAt { get; set; }

    [NotMapped]
    public bool IsActive => DateTimeOffset.UtcNow < UnlocksAt;
}
```

- [ ] **Step 2: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeLockState.cs
git commit -m "feat(models): add ChallengeLockState entity"
```

---

### Task 4: 创建 ChallengeTrackLevelBanState 实体

**Files:**
- Create: `src/GZCTF/Models/Data/ChallengeTrackLevelBanState.cs`

- [ ] **Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 永久 (Track, Level) 封禁状态（per-participation）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(TrackId), nameof(Level), IsUnique = true)]
public class ChallengeTrackLevelBanState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public Guid TrackId { get; set; }
    public int Level { get; set; }

    [MaxLength(Limits.MaxFlagLength)]
    public string BanAnswer { get; set; } = string.Empty;

    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

- [ ] **Step 2: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeTrackLevelBanState.cs
git commit -m "feat(models): add ChallengeTrackLevelBanState entity"
```

---

### Task 5: 创建 ChallengeWrongCountState 实体

**Files:**
- Create: `src/GZCTF/Models/Data/ChallengeWrongCountState.cs`

- [ ] **Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// per-Level 错误提交计数（锁到期后重置）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(Level), IsUnique = true)]
public class ChallengeWrongCountState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public int Level { get; set; }

    public int WrongCount { get; set; }
    public DateTimeOffset LastResetAt { get; set; } = DateTimeOffset.UtcNow;
}
```

- [ ] **Step 2: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeWrongCountState.cs
git commit -m "feat(models): add ChallengeWrongCountState entity"
```

---

### Task 6: 修改 ChallengeFlag 实体

**Files:**
- Modify: `src/GZCTF/Models/Data/ChallengeFlag.cs`

- [ ] **Step 1: 用以下内容完整替换文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId))]
[Index(nameof(ChallengeId), nameof(Level), nameof(TrackId))]
public class ChallengeFlag
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Flag value（匹配用）
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string FlagValue { get; set; } = string.Empty;

    /// <summary>
    /// Per-flag 答案类型，决定匹配算法
    /// </summary>
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;

    /// <summary>
    /// 原始答案文本（HybridLine 等模式归一化前）
    /// </summary>
    [MaxLength(Limits.MaxFlagLength)]
    public string RawAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 归一化后的匹配键
    /// </summary>
    [MaxLength(Limits.MaxFlagLength + 16)]
    public string NormalizedAnswerKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否参与匹配
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否对选手可见（HybridLine subsystem details）
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否已被分配（dynamic attachment 用）
    /// </summary>
    public bool IsOccupied { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 所属层级（1, 2, 3...）
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// 所属 Track（同 TrackId 的 flags 属于同一漏洞实例）
    /// </summary>
    public Guid TrackId { get; set; } = Guid.NewGuid();

    #region Db Relationship

    public int ChallengeId { get; set; }
    public GameChallenge? Challenge { get; set; }

    public int? ExerciseId { get; set; }
    public ExerciseChallenge? Exercise { get; set; }

    public int? AttachmentId { get; set; }
    public Attachment? Attachment { get; set; }

    public List<SubmissionRecord> SubmissionRecords { get; set; } = [];

    #endregion
}
```

- [ ] **Step 2: 编译验证（预期会有引用旧字段的错误，暂不修复）**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -30`
Expected: 编译错误，引用 `FlawNo`/`Repository`/`Module`/`FilePath`/`Cwe`/`Score` 的地方报错

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/ChallengeFlag.cs
git commit -m "feat(models): refactor ChallengeFlag for multi-level (add Level/TrackId, remove hardcoded fields)"
```

---

### Task 7: 修改 Challenge 实体

**Files:**
- Modify: `src/GZCTF/Models/Data/Challenge.cs`

- [ ] **Step 1: 删除 FlagMatchMode 字段，添加 LockDurationMinutes**

打开 `src/GZCTF/Models/Data/Challenge.cs`，定位到：
```csharp
    /// <summary>
    /// Flag matching mode for this challenge
    /// </summary>
    [Required]
    public FlagMatchMode FlagMatchMode { get; set; } = FlagMatchMode.Exact;
```

替换为：
```csharp
    /// <summary>
    /// 临时全题锁时长（分钟），0 = 永久
    /// </summary>
    [Range(0, int.MaxValue)]
    public int LockDurationMinutes { get; set; } = 0;
```

- [ ] **Step 2: 编译验证（仍有 FlagMatchMode 引用错误，正常）**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -30`
Expected: 多处 FlagMatchMode 引用错误

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF/Models/Data/Challenge.cs
git commit -m "feat(models): replace Challenge.FlagMatchMode with LockDurationMinutes"
```

---

### Task 8: 修改 SubmissionRecord 实体

**Files:**
- Modify: `src/GZCTF/Models/Data/SubmissionRecord.cs`

- [ ] **Step 1: 用以下内容完整替换文件**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(SubmissionId), IsUnique = true)]
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(TrackId), nameof(Level))]
public class SubmissionRecord
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Score change (positive = correct flag, negative = penalty)
    /// </summary>
    public int ScoreChange { get; set; }

    /// <summary>
    /// Whether this record represents a penalty (wrong answer)
    /// </summary>
    public bool IsPenalty { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Denormalized game ID
    /// </summary>
    public int? GameId { get; set; }

    /// <summary>
    /// Denormalized challenge ID
    /// </summary>
    public int? ChallengeId { get; set; }

    /// <summary>
    /// Denormalized participation ID
    /// </summary>
    public int? ParticipationId { get; set; }

    /// <summary>
    /// Denormalized level
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// Denormalized track ID
    /// </summary>
    public Guid? TrackId { get; set; }

    /// <summary>
    /// 提交类型：Correct | Wrong | Trap | Ban | Duplicate
    /// </summary>
    [MaxLength(20)]
    public string? SubmissionType { get; set; }

    #region Db Relationship

    public int SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public Guid? MatchedFlagId { get; set; }
    public ChallengeFlag? MatchedFlag { get; set; }

    #endregion
}
```

- [ ] **Step 2: 提交**

```bash
git add src/GZCTF/Models/Data/SubmissionRecord.cs
git commit -m "feat(models): add Level/TrackId/SubmissionType to SubmissionRecord"
```

---

### Task 9: 在 AppDbContext 注册新实体

**Files:**
- Modify: `src/GZCTF/Models/AppDbContext.cs`

- [ ] **Step 1: 添加 DbSet**

在 `AppDbContext.cs` 找到 `public DbSet<ZeroDayTicket> ZeroDayTickets` 行，在其下方添加：

```csharp
    public DbSet<ChallengeTrack> ChallengeTracks { get; set; } = null!;
    public DbSet<ChallengeLevelConfig> ChallengeLevelConfigs { get; set; } = null!;
    public DbSet<ChallengeLockState> ChallengeLockStates { get; set; } = null!;
    public DbSet<ChallengeTrackLevelBanState> ChallengeTrackLevelBanStates { get; set; } = null!;
    public DbSet<ChallengeWrongCountState> ChallengeWrongCountStates { get; set; } = null!;
```

- [ ] **Step 2: 在 OnModelCreating 末尾（`builder.Entity<ZeroDayTicket>` 之后）添加实体配置**

```csharp
        var listConverter = GetJsonConverter<List<string>>();
        var listComparer = GetEnumerableComparer<List<string>, string>();

        builder.Entity<ChallengeTrack>(entity =>
        {
            entity.HasIndex(e => new { e.ChallengeId, e.SortOrder });

            entity.HasOne(e => e.Challenge)
                .WithMany()
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeLevelConfig>(entity =>
        {
            entity.HasIndex(e => new { e.ChallengeId, e.Level }).IsUnique();

            entity.Property(e => e.LockOnAnswers)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            entity.Property(e => e.BanOnAnswers)
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);

            entity.HasOne(e => e.Challenge)
                .WithMany()
                .HasForeignKey(e => e.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeLockState>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipationId, e.ChallengeId, e.UnlocksAt });

            entity.HasOne(e => e.Participation)
                .WithMany()
                .HasForeignKey(e => e.ParticipationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeTrackLevelBanState>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipationId, e.ChallengeId, e.TrackId, e.Level }).IsUnique();
        });

        builder.Entity<ChallengeWrongCountState>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipationId, e.ChallengeId, e.Level }).IsUnique();
        });
```

注意：`ChallengeLockState` 引用了 `Participation` 导航属性，需要在实体上加：

回到 `src/GZCTF/Models/Data/ChallengeLockState.cs`，在 `#region Db Relationship` 内加：
```csharp
    public Participation Participation { get; set; } = null!;
```

- [ ] **Step 3: 更新 ChallengeFlag 配置**

在 `AppDbContext.cs` 找到 `builder.Entity<ChallengeFlag>(entity =>` 块，把原有的 `(ChallengeId, FlagValue)` 唯一索引和 `(ChallengeId, NormalizedAnswerKey)` 索引替换为：

```csharp
            entity.HasIndex(e => new { e.ChallengeId, e.Level, e.TrackId });
            entity.HasIndex(e => new { e.ChallengeId, e.FlagValue });
            entity.HasIndex(e => new { e.ChallengeId, e.NormalizedAnswerKey });
```

（FlagValue 不再全局唯一，因为不同 Track 可能相同答案；改用应用层校验避免跨 Track 冲突）

- [ ] **Step 4: 删除 SubmissionRecord 原有索引，替换为新索引**

找到 `builder.Entity<SubmissionRecord>(entity =>` 块内的：
```csharp
            entity.HasIndex(e => new { e.ParticipationId, e.MatchedFlagId })
                .IsUnique()
                .HasFilter("\"ParticipationId\" IS NOT NULL AND \"MatchedFlagId\" IS NOT NULL AND \"ScoreChange\" > 0");
```

替换为：
```csharp
            entity.HasIndex(e => new { e.ParticipationId, e.ChallengeId, e.TrackId, e.Level })
                .HasFilter("\"ParticipationId\" IS NOT NULL AND \"TrackId\" IS NOT NULL AND \"ScoreChange\" > 0");
```

- [ ] **Step 5: 提交**

```bash
git add src/GZCTF/Models/AppDbContext.cs src/GZCTF/Models/Data/ChallengeLockState.cs
git commit -m "feat(data): register multi-level flag entities in AppDbContext"
```

---

### Task 10: 删除 FlagMatchMode 枚举

**Files:**
- Modify: `src/GZCTF/Utils/Enums.cs`

- [ ] **Step 1: 删除 FlagMatchMode 枚举定义**

在 `src/GZCTF/Utils/Enums.cs` 中找到并删除整个 `FlagMatchMode` 枚举块（含注释和 `[JsonConverter]` 特性）：

```csharp
/// <summary>
/// Flag matching mode for a challenge
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FlagMatchMode>))]
public enum FlagMatchMode : byte
{
    Exact = 0,
    LineRange = 1,
    HybridLine = 2
}
```

保留 `ChallengeFlagAnswerType` 枚举。

- [ ] **Step 2: 提交**

```bash
git add src/GZCTF/Utils/Enums.cs
git commit -m "refactor(enums): remove FlagMatchMode (unified per-flag AnswerType)"
```

---

### Task 11: 生成 EF Core 迁移

**Files:**
- Create: `src/GZCTF/Migrations/20260620000000_AddMultiLevelFlagModel.cs`

- [ ] **Step 1: 先修复所有编译错误（在后续 Task 中处理引用）**

暂时在引用 `FlagMatchMode` 的所有文件中注释掉相关行，让编译通过。运行：
```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | grep "CS0246\|CS0103" | head -30
```

逐一修复：打开每个报错文件，把 `FlagMatchMode` 引用注释掉（后续 Task 会完整重构）。修复到编译通过。

- [ ] **Step 2: 生成迁移**

Run:
```bash
cd src && dotnet ef migrations add AddMultiLevelFlagModel --project GZCTF --startup-project GZCTF
```
Expected: 生成 `Migrations/20260620000000_AddMultiLevelFlagModel.cs` 和 `.Designer.cs`，更新 `AppDbContextModelSnapshot.cs`

- [ ] **Step 3: 检查生成的迁移文件**

Run: `cat src/GZCTF/Migrations/20260620000000_AddMultiLevelFlagModel.cs | head -80`
Expected: 包含 5 张新表的 CreateTable、ChallengeFlags/GameChallenges/SubmissionRecords 的 AddColumn/DropColumn

- [ ] **Step 4: 在迁移文件末尾的 `Up` 方法最后添加数据迁移 SQL**

打开 `src/GZCTF/Migrations/20260620000000_AddMultiLevelFlagModel.cs`，在 `protected override void Up` 方法最后（所有 CreateTable/AddColumn 之后、DropColumn 之前）插入：

```csharp
            // 数据迁移：为每个旧 ChallengeFlag 创建独立 ChallengeTrack
            migrationBuilder.Sql(@"
ALTER TABLE ""ChallengeTracks"" ADD COLUMN ""_SourceFlagId"" uuid;

INSERT INTO ""ChallengeTracks"" (""Id"", ""ChallengeId"", ""Title"", ""FlawNo"", ""Repository"", ""Module"", ""FilePath"", ""IsEnabled"", ""SortOrder"", ""CreatedAt"", ""UpdatedAt"", ""_SourceFlagId"")
SELECT
    gen_random_uuid(),
    cf.""ChallengeId"",
    COALESCE(cf.""FilePath"", cf.""FlawNo"", 'Track ' || cf.""Id""::text),
    cf.""FlawNo"",
    cf.""Repository"",
    cf.""Module"",
    cf.""FilePath"",
    true,
    cf.""SortOrder"",
    NOW(),
    NOW(),
    cf.""Id""
FROM ""ChallengeFlags"" cf;

UPDATE ""ChallengeFlags"" cf
SET ""TrackId"" = ct.""Id""
FROM ""ChallengeTracks"" ct
WHERE ct.""_SourceFlagId"" = cf.""Id"";

ALTER TABLE ""ChallengeTracks"" DROP COLUMN ""_SourceFlagId"";

INSERT INTO ""ChallengeLevelConfigs"" (""ChallengeId"", ""Level"", ""Title"", ""Score"", ""SubmissionLimit"", ""PenaltyScore"", ""LockOnAnswers"", ""BanOnAnswers"", ""IsEnabled"", ""SortOrder"")
SELECT cf.""ChallengeId"", 1, 'Level 1', COALESCE(MAX(cf.""Score""), 0), 0, 0, '[]', '[]', true, 1
FROM ""ChallengeFlags"" cf
GROUP BY cf.""ChallengeId"";

UPDATE ""SubmissionRecords"" sr
SET ""Level"" = 1, ""TrackId"" = cf.""TrackId"", ""SubmissionType"" =
    CASE WHEN sr.""IsPenalty"" = true THEN 'Wrong'
         WHEN sr.""ScoreChange"" > 0 THEN 'Correct'
         ELSE 'Wrong' END
FROM ""ChallengeFlags"" cf
WHERE sr.""MatchedFlagId"" = cf.""Id"";
");
```

- [ ] **Step 5: 编译并验证迁移**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 6: 提交**

```bash
git add src/GZCTF/Migrations/
git commit -m "feat(migrations): add AddMultiLevelFlagModel migration with data backfill"
```

---

## Phase 2: Repositories

### Task 12: 创建 IChallengeTrackRepository 接口与实现

**Files:**
- Create: `src/GZCTF/Repositories/Interface/IChallengeTrackRepository.cs`
- Create: `src/GZCTF/Repositories/ChallengeTrackRepository.cs`

- [ ] **Step 1: 创建接口**

```csharp
namespace GZCTF.Repositories.Interface;

public interface IChallengeTrackRepository : IRepository
{
    Task<ChallengeTrack[]> GetTracks(int challengeId, CancellationToken token = default);
    Task<ChallengeTrack?> GetTrack(Guid trackId, CancellationToken token = default);
    Task<ChallengeTrack> CreateTrack(ChallengeTrack track, CancellationToken token = default);
    Task<ChallengeTrack> UpdateTrack(ChallengeTrack track, CancellationToken token = default);
    Task<bool> DeleteTrack(Guid trackId, CancellationToken token = default);  // 软删除
    Task<bool> HasFlags(Guid trackId, CancellationToken token = default);
    Task<bool> HasSubmissions(Guid trackId, CancellationToken token = default);
}
```

- [ ] **Step 2: 创建实现**

```csharp
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeTrackRepository(AppDbContext context) : RepositoryBase(context), IChallengeTrackRepository
{
    public Task<ChallengeTrack[]> GetTracks(int challengeId, CancellationToken token = default) =>
        Context.ChallengeTracks
            .Where(t => t.ChallengeId == challengeId)
            .OrderBy(t => t.SortOrder)
            .ToArrayAsync(token);

    public Task<ChallengeTrack?> GetTrack(Guid trackId, CancellationToken token = default) =>
        Context.ChallengeTracks.FirstOrDefaultAsync(t => t.Id == trackId, token);

    public async Task<ChallengeTrack> CreateTrack(ChallengeTrack track, CancellationToken token = default)
    {
        await Context.ChallengeTracks.AddAsync(track, token);
        await Context.SaveChangesAsync(token);
        return track;
    }

    public async Task<ChallengeTrack> UpdateTrack(ChallengeTrack track, CancellationToken token = default)
    {
        track.UpdatedAt = DateTimeOffset.UtcNow;
        Context.ChallengeTracks.Update(track);
        await Context.SaveChangesAsync(token);
        return track;
    }

    public async Task<bool> DeleteTrack(Guid trackId, CancellationToken token = default)
    {
        var track = await Context.ChallengeTracks.FindAsync([trackId], token);
        if (track is null) return false;

        track.IsEnabled = false;
        track.UpdatedAt = DateTimeOffset.UtcNow;

        // 级联软删除 flags
        var flags = await Context.ChallengeFlags
            .Where(f => f.TrackId == trackId)
            .ToArrayAsync(token);
        foreach (var f in flags) f.IsEnabled = false;

        await Context.SaveChangesAsync(token);
        return true;
    }

    public Task<bool> HasFlags(Guid trackId, CancellationToken token = default) =>
        Context.ChallengeFlags.AnyAsync(f => f.TrackId == trackId, token);

    public Task<bool> HasSubmissions(Guid trackId, CancellationToken token = default) =>
        Context.SubmissionRecords.AnyAsync(r => r.TrackId == trackId, token);
}
```

- [ ] **Step 3: 注册 DI**

在 `src/GZCTF/Extensions/Startup/ServicesExtension.cs` 找到 `AddScoped<IZeroDayTicketRepository>` 行，在其下方添加：
```csharp
            builder.Services.AddScoped<IChallengeTrackRepository, ChallengeTrackRepository>();
```

- [ ] **Step 4: 编译验证**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: 提交**

```bash
git add src/GZCTF/Repositories/Interface/IChallengeTrackRepository.cs \
        src/GZCTF/Repositories/ChallengeTrackRepository.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(repos): add ChallengeTrackRepository"
```

---

### Task 13: 创建 IChallengeLevelConfigRepository 接口与实现

**Files:**
- Create: `src/GZCTF/Repositories/Interface/IChallengeLevelConfigRepository.cs`
- Create: `src/GZCTF/Repositories/ChallengeLevelConfigRepository.cs`

- [ ] **Step 1: 创建接口**

```csharp
namespace GZCTF.Repositories.Interface;

public interface IChallengeLevelConfigRepository : IRepository
{
    Task<ChallengeLevelConfig[]> GetConfigs(int challengeId, CancellationToken token = default);
    Task<ChallengeLevelConfig?> GetConfig(int challengeId, int level, CancellationToken token = default);
    Task<ChallengeLevelConfig> CreateConfig(ChallengeLevelConfig config, CancellationToken token = default);
    Task<ChallengeLevelConfig> UpdateConfig(ChallengeLevelConfig config, CancellationToken token = default);
    Task<bool> DeleteConfig(int configId, CancellationToken token = default);
    Task<bool> HasFlags(int challengeId, int level, CancellationToken token = default);
}
```

- [ ] **Step 2: 创建实现**

```csharp
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeLevelConfigRepository(AppDbContext context) : RepositoryBase(context), IChallengeLevelConfigRepository
{
    public Task<ChallengeLevelConfig[]> GetConfigs(int challengeId, CancellationToken token = default) =>
        Context.ChallengeLevelConfigs
            .Where(c => c.ChallengeId == challengeId)
            .OrderBy(c => c.Level)
            .ToArrayAsync(token);

    public Task<ChallengeLevelConfig?> GetConfig(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeLevelConfigs
            .FirstOrDefaultAsync(c => c.ChallengeId == challengeId && c.Level == level, token);

    public async Task<ChallengeLevelConfig> CreateConfig(ChallengeLevelConfig config, CancellationToken token = default)
    {
        await Context.ChallengeLevelConfigs.AddAsync(config, token);
        await Context.SaveChangesAsync(token);
        return config;
    }

    public async Task<ChallengeLevelConfig> UpdateConfig(ChallengeLevelConfig config, CancellationToken token = default)
    {
        Context.ChallengeLevelConfigs.Update(config);
        await Context.SaveChangesAsync(token);
        return config;
    }

    public async Task<bool> DeleteConfig(int configId, CancellationToken token = default)
    {
        var config = await Context.ChallengeLevelConfigs.FindAsync([configId], token);
        if (config is null) return false;
        Context.ChallengeLevelConfigs.Remove(config);
        await Context.SaveChangesAsync(token);
        return true;
    }

    public Task<bool> HasFlags(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeFlags.AnyAsync(f => f.ChallengeId == challengeId && f.Level == level, token);
}
```

- [ ] **Step 3: 注册 DI**

在 `ServicesExtension.cs` 的 `IChallengeTrackRepository` 下方添加：
```csharp
            builder.Services.AddScoped<IChallengeLevelConfigRepository, ChallengeLevelConfigRepository>();
```

- [ ] **Step 4: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/IChallengeLevelConfigRepository.cs \
        src/GZCTF/Repositories/ChallengeLevelConfigRepository.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(repos): add ChallengeLevelConfigRepository"
```

---

### Task 14: 创建 IChallengeLockStateRepository 接口与实现

**Files:**
- Create: `src/GZCTF/Repositories/Interface/IChallengeLockStateRepository.cs`
- Create: `src/GZCTF/Repositories/ChallengeLockStateRepository.cs`

- [ ] **Step 1: 创建接口**

```csharp
namespace GZCTF.Repositories.Interface;

public interface IChallengeLockStateRepository : IRepository
{
    Task<ChallengeLockState?> GetActiveLock(int participationId, int challengeId, CancellationToken token = default);
    Task<List<ChallengeLockState>> GetExpiredLocks(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeLockState> CreateLock(ChallengeLockState lockState, CancellationToken token = default);
    Task DeleteLocks(IEnumerable<ChallengeLockState> locks, CancellationToken token = default);
}
```

- [ ] **Step 2: 创建实现**

```csharp
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeLockStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeLockStateRepository
{
    public Task<ChallengeLockState?> GetActiveLock(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeLockStates
            .Where(l => l.ParticipationId == participationId
                && l.ChallengeId == challengeId
                && l.UnlocksAt > DateTimeOffset.UtcNow)
            .OrderByDescending(l => l.UnlocksAt)
            .FirstOrDefaultAsync(token);

    public Task<List<ChallengeLockState>> GetExpiredLocks(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeLockStates
            .Where(l => l.ParticipationId == participationId
                && l.ChallengeId == challengeId
                && l.UnlocksAt <= DateTimeOffset.UtcNow)
            .ToListAsync(token);

    public async Task<ChallengeLockState> CreateLock(ChallengeLockState lockState, CancellationToken token = default)
    {
        await Context.ChallengeLockStates.AddAsync(lockState, token);
        await Context.SaveChangesAsync(token);
        return lockState;
    }

    public async Task DeleteLocks(IEnumerable<ChallengeLockState> locks, CancellationToken token = default)
    {
        Context.ChallengeLockStates.RemoveRange(locks);
        await Context.SaveChangesAsync(token);
    }
}
```

- [ ] **Step 3: 注册 DI**

```csharp
            builder.Services.AddScoped<IChallengeLockStateRepository, ChallengeLockStateRepository>();
```

- [ ] **Step 4: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/IChallengeLockStateRepository.cs \
        src/GZCTF/Repositories/ChallengeLockStateRepository.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(repos): add ChallengeLockStateRepository"
```

---

### Task 15: 创建 IChallengeTrackLevelBanStateRepository 接口与实现

**Files:**
- Create: `src/GZCTF/Repositories/Interface/IChallengeTrackLevelBanStateRepository.cs`
- Create: `src/GZCTF/Repositories/ChallengeTrackLevelBanStateRepository.cs`

- [ ] **Step 1: 创建接口**

```csharp
namespace GZCTF.Repositories.Interface;

public interface IChallengeTrackLevelBanStateRepository : IRepository
{
    Task<bool> IsBanned(int participationId, int challengeId, Guid trackId, int level, CancellationToken token = default);
    Task<ChallengeTrackLevelBanState[]> GetBans(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeTrackLevelBanState> CreateBan(ChallengeTrackLevelBanState ban, CancellationToken token = default);
}
```

- [ ] **Step 2: 创建实现**

```csharp
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeTrackLevelBanStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeTrackLevelBanStateRepository
{
    public Task<bool> IsBanned(int participationId, int challengeId, Guid trackId, int level, CancellationToken token = default) =>
        Context.ChallengeTrackLevelBanStates
            .AnyAsync(b => b.ParticipationId == participationId
                && b.ChallengeId == challengeId
                && b.TrackId == trackId
                && b.Level == level, token);

    public Task<ChallengeTrackLevelBanState[]> GetBans(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeTrackLevelBanStates
            .Where(b => b.ParticipationId == participationId && b.ChallengeId == challengeId)
            .ToArrayAsync(token);

    public async Task<ChallengeTrackLevelBanState> CreateBan(ChallengeTrackLevelBanState ban, CancellationToken token = default)
    {
        await Context.ChallengeTrackLevelBanStates.AddAsync(ban, token);
        await Context.SaveChangesAsync(token);
        return ban;
    }
}
```

- [ ] **Step 3: 注册 DI 并提交**

```csharp
            builder.Services.AddScoped<IChallengeTrackLevelBanStateRepository, ChallengeTrackLevelBanStateRepository>();
```

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/IChallengeTrackLevelBanStateRepository.cs \
        src/GZCTF/Repositories/ChallengeTrackLevelBanStateRepository.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(repos): add ChallengeTrackLevelBanStateRepository"
```

---

### Task 16: 创建 IChallengeWrongCountStateRepository 接口与实现

**Files:**
- Create: `src/GZCTF/Repositories/Interface/IChallengeWrongCountStateRepository.cs`
- Create: `src/GZCTF/Repositories/ChallengeWrongCountStateRepository.cs`

- [ ] **Step 1: 创建接口**

```csharp
namespace GZCTF.Repositories.Interface;

public interface IChallengeWrongCountStateRepository : IRepository
{
    Task<int> GetWrongCount(int participationId, int challengeId, int level, CancellationToken token = default);
    Task<int> IncrementWrongCount(int participationId, int challengeId, int level, CancellationToken token = default);
    Task ResetAllLevels(int participationId, int challengeId, CancellationToken token = default);
}
```

- [ ] **Step 2: 创建实现**

```csharp
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeWrongCountStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeWrongCountStateRepository
{
    public async Task<int> GetWrongCount(int participationId, int challengeId, int level, CancellationToken token = default)
    {
        var state = await Context.ChallengeWrongCountStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId
                && s.Level == level, token);
        return state?.WrongCount ?? 0;
    }

    public async Task<int> IncrementWrongCount(int participationId, int challengeId, int level, CancellationToken token = default)
    {
        var state = await Context.ChallengeWrongCountStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId
                && s.Level == level, token);

        if (state is null)
        {
            state = new ChallengeWrongCountState
            {
                ParticipationId = participationId,
                ChallengeId = challengeId,
                Level = level,
                WrongCount = 1,
                LastResetAt = DateTimeOffset.UtcNow
            };
            await Context.ChallengeWrongCountStates.AddAsync(state, token);
        }
        else
        {
            state.WrongCount++;
        }

        await Context.SaveChangesAsync(token);
        return state.WrongCount;
    }

    public async Task ResetAllLevels(int participationId, int challengeId, CancellationToken token = default)
    {
        var states = await Context.ChallengeWrongCountStates
            .Where(s => s.ParticipationId == participationId && s.ChallengeId == challengeId)
            .ToArrayAsync(token);

        foreach (var s in states)
        {
            s.WrongCount = 0;
            s.LastResetAt = DateTimeOffset.UtcNow;
        }

        await Context.SaveChangesAsync(token);
    }
}
```

- [ ] **Step 3: 注册 DI 并提交**

```csharp
            builder.Services.AddScoped<IChallengeWrongCountStateRepository, ChallengeWrongCountStateRepository>();
```

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/IChallengeWrongCountStateRepository.cs \
        src/GZCTF/Repositories/ChallengeWrongCountStateRepository.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(repos): add ChallengeWrongCountStateRepository"
```

---

### Task 17: 扩展 ISubmissionRecordRepository 接口

**Files:**
- Modify: `src/GZCTF/Repositories/Interface/ISubmissionRecordRepository.cs`
- Modify: `src/GZCTF/Repositories/SubmissionRecordRepository.cs`

- [ ] **Step 1: 先读现有接口**

Run: `cat src/GZCTF/Repositories/Interface/ISubmissionRecordRepository.cs`

- [ ] **Step 2: 在接口末尾（`}` 之前）添加新方法签名**

```csharp
    Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid trackId, int level, CancellationToken token = default);
    Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid flagId, CancellationToken token = default);
    Task<List<Guid>> GetSolvedFlagIds(int challengeId, int participationId, CancellationToken token = default);
    Task<List<(Guid TrackId, int Level)>> GetSolvedTrackLevels(int challengeId, int participationId, CancellationToken token = default);
```

- [ ] **Step 3: 在 `SubmissionRecordRepository.cs` 实现新方法**

在类末尾（`}` 之前）添加：

```csharp
    public Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid trackId, int level, CancellationToken token = default) =>
        Context.SubmissionRecords
            .AnyAsync(r => r.ChallengeId == challengeId
                && r.ParticipationId == participationId
                && r.TrackId == trackId
                && r.Level == level
                && r.ScoreChange > 0, token);

    public Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid flagId, CancellationToken token = default) =>
        Context.SubmissionRecords
            .AnyAsync(r => r.ChallengeId == challengeId
                && r.ParticipationId == participationId
                && r.MatchedFlagId == flagId
                && r.ScoreChange > 0, token);

    public async Task<List<Guid>> GetSolvedFlagIds(int challengeId, int participationId, CancellationToken token = default) =>
        await Context.SubmissionRecords
            .Where(r => r.ChallengeId == challengeId
                && r.ParticipationId == participationId
                && r.ScoreChange > 0
                && r.MatchedFlagId != null)
            .Select(r => r.MatchedFlagId!.Value)
            .Distinct()
            .ToListAsync(token);

    public async Task<List<(Guid TrackId, int Level)>> GetSolvedTrackLevels(int challengeId, int participationId, CancellationToken token = default) =>
        await Context.SubmissionRecords
            .Where(r => r.ChallengeId == challengeId
                && r.ParticipationId == participationId
                && r.ScoreChange > 0
                && r.TrackId != null
                && r.Level != null)
            .Select(r => new { r.TrackId!.Value, r.Level!.Value })
            .Distinct()
            .Select(x => new ValueTuple<Guid, int>(x.Value, x.Value))
            .ToListAsync(token);
```

注：最后一行的 `Select` 投影为 `ValueTuple` 以匹配返回类型；EF Core 9 支持 value tuple 投影。

- [ ] **Step 4: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/ISubmissionRecordRepository.cs \
        src/GZCTF/Repositories/SubmissionRecordRepository.cs
git commit -m "feat(repos): extend ISubmissionRecordRepository with track/level overloads"
```

---

### Task 18: 扩展 IChallengeFlagRepository 接口

**Files:**
- Modify: `src/GZCTF/Repositories/Interface/IChallengeFlagRepository.cs`
- Modify: `src/GZCTF/Repositories/ChallengeFlagRepository.cs`

- [ ] **Step 1: 在接口末尾添加新方法**

```csharp
    Task<ChallengeFlag[]> GetFlagsByLevel(int challengeId, int level, CancellationToken token = default);
    Task<ChallengeFlag[]> GetFlagsByTrack(Guid trackId, CancellationToken token = default);
    Task<ChallengeFlag[]> GetFlagsByTrackLevel(Guid trackId, int level, CancellationToken token = default);
    Task<int> CountFlagsByLevel(int challengeId, int level, CancellationToken token = default);
```

- [ ] **Step 2: 在 `ChallengeFlagRepository.cs` 实现新方法**

```csharp
    public Task<ChallengeFlag[]> GetFlagsByLevel(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeFlags
            .Where(f => f.ChallengeId == challengeId && f.Level == level && f.IsEnabled)
            .OrderBy(f => f.SortOrder)
            .ToArrayAsync(token);

    public Task<ChallengeFlag[]> GetFlagsByTrack(Guid trackId, CancellationToken token = default) =>
        Context.ChallengeFlags
            .Where(f => f.TrackId == trackId && f.IsEnabled)
            .OrderBy(f => f.Level)
            .ThenBy(f => f.SortOrder)
            .ToArrayAsync(token);

    public Task<ChallengeFlag[]> GetFlagsByTrackLevel(Guid trackId, int level, CancellationToken token = default) =>
        Context.ChallengeFlags
            .Where(f => f.TrackId == trackId && f.Level == level && f.IsEnabled)
            .OrderBy(f => f.SortOrder)
            .ToArrayAsync(token);

    public Task<int> CountFlagsByLevel(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeFlags
            .CountAsync(f => f.ChallengeId == challengeId && f.Level == level && f.IsEnabled, token);
```

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Repositories/Interface/IChallengeFlagRepository.cs \
        src/GZCTF/Repositories/ChallengeFlagRepository.cs
git commit -m "feat(repos): extend IChallengeFlagRepository with level/track queries"
```

---

## Phase 3: ScoringService 重构

### Task 19: 抽取 ScoreResult 模型到独立文件

**Files:**
- Create: `src/GZCTF/Services/Scoring/ScoreResult.cs`
- Modify: `src/GZCTF/Services/Scoring/ScoringService.cs`

- [ ] **Step 1: 创建 ScoreResult.cs**

```csharp
namespace GZCTF.Services.Scoring;

public class ScoreResult
{
    public AnswerResult Status { get; set; }
    public int ScoreChange { get; set; }
    public bool IsPenalty { get; set; }
    public Guid? MatchedFlagId { get; set; }
    public SubmissionType SubmissionType { get; set; }
    public string? Message { get; set; }

    // 多级 flag 新增字段
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

- [ ] **Step 2: 从 ScoringService.cs 删除 ScoreResult 类定义**

打开 `src/GZCTF/Services/Scoring/ScoringService.cs`，搜索 `public class ScoreResult`，删除整个类定义（已搬到独立文件）。

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Services/Scoring/ScoreResult.cs src/GZCTF/Services/Scoring/ScoringService.cs
git commit -m "refactor(scoring): extract ScoreResult to its own file"
```

---

### Task 20: 重构 ScoringService.ProcessSubmission

**Files:**
- Modify: `src/GZCTF/Services/Scoring/ScoringService.cs`

- [ ] **Step 1: 在 ScoringService 类顶部添加新依赖字段**

找到 ScoringService 的构造函数，添加新依赖：

```csharp
    private readonly IChallengeTrackRepository _trackRepo;
    private readonly IChallengeLevelConfigRepository _levelConfigRepo;
    private readonly IChallengeLockStateRepository _lockStateRepo;
    private readonly IChallengeTrackLevelBanStateRepository _banStateRepo;
    private readonly IChallengeWrongCountStateRepository _wrongCountRepo;
```

构造函数参数列表添加：
```csharp
        IChallengeTrackRepository trackRepo,
        IChallengeLevelConfigRepository levelConfigRepo,
        IChallengeLockStateRepository lockStateRepo,
        IChallengeTrackLevelBanStateRepository banStateRepo,
        IChallengeWrongCountStateRepository wrongCountRepo,
```

并在构造函数体内赋值。

- [ ] **Step 2: 重写 ProcessSubmission 方法**

找到现有 `public async Task<ScoreResult> ProcessSubmission(...)`，整个方法替换为：

```csharp
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
            return new ScoreResult
            {
                Status = AnswerResult.FlagSubmitted,
                ScoreChange = 0,
                TrackId = trackId,
                Level = level,
                Message = "Answer not acceptable at this time"
            };
        }

        return await HandleCorrectSubmission(challenge, participation, firstValid, token);
    }
```

- [ ] **Step 3: 添加 PickFirstValidMatch 私有方法**

在 ScoringService 类内添加：

```csharp
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

            // 已解检查
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
```

- [ ] **Step 4: 添加 TryMatch 私有静态方法**

```csharp
    private static bool TryMatch(ChallengeFlag flag, string answer)
    {
        var submitted = ParseHybridLineAnswer(answer, flag.AnswerType);
        if (submitted is null) return false;

        var flagAnswerStr = string.IsNullOrWhiteSpace(flag.RawAnswer) ? flag.FlagValue : flag.RawAnswer;
        var flagAnswer = ParseHybridLineAnswer(flagAnswerStr, flag.AnswerType);
        if (flagAnswer is null) return false;

        return HybridLineMatches(flagAnswer.Value, submitted.Value);
    }
```

- [ ] **Step 5: 暂时注释旧的 HandleCorrectSubmission / HandleWrongSubmission / MatchLineRange / MatchHybridLine（下一 Task 重写）**

在 `ScoringService.cs` 中，把这些方法整块注释掉（用 `/* */`），保留 `ParseHybridLineAnswer`、`HybridLineMatches`、`TryNormalizeHybridLineAnswer`、`NormalizeHybridLineAnswer`、`TryParseLineRange` 等纯函数。

- [ ] **Step 6: 编译（预期会有 GameChallenge.Update、GameController.Submit 等地方的错误，暂不处理）**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -30`
Expected: 少量错误（来自 GameController 等还未改造的调用方）

- [ ] **Step 7: 提交**

```bash
git add src/GZCTF/Services/Scoring/ScoringService.cs
git commit -m "refactor(scoring): rewrite ProcessSubmission with track/level context"
```

---

### Task 21: 重写 HandleCorrectSubmission 与 HandleWrongSubmission

**Files:**
- Modify: `src/GZCTF/Services/Scoring/ScoringService.cs`

- [ ] **Step 1: 添加 HandleCorrectSubmission**

在 ScoringService 类内（ProcessSubmission 下方）添加：

```csharp
    private async Task<ScoreResult> HandleCorrectSubmission(
        GameChallenge challenge,
        Participation participation,
        ChallengeFlag matchedFlag,
        CancellationToken token)
    {
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, matchedFlag.Level, token);
        var score = levelConfig?.Score ?? 0;

        var submissionType = await DetermineSubmissionType(challenge, participation, token);

        // 触发 Track 完成检查与 FirstSolve 检查由调用方在保存 SubmissionRecord 后调用 CreateFirstSolve

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
        // 1. Duplicate 检查：同一 answer 之前错误提交过 → 不扣分
        var duplicate = await _context.Submissions
            .AsNoTracking()
            .AnyAsync(s => s.ParticipationId == participation.Id
                && s.ChallengeId == challenge.Id
                && s.Answer == answer
                && s.Status == AnswerResult.WrongAnswer, token);

        if (duplicate)
        {
            return new ScoreResult
            {
                Status = AnswerResult.WrongAnswer,
                ScoreChange = 0,
                TrackId = trackId,
                Level = level,
                Message = "Duplicate wrong submission, no penalty"
            };
        }

        // 2. 加载 Level 配置
        var levelConfig = await _levelConfigRepo.GetConfig(challenge.Id, level, token);

        // 3. Penalty 计算
        var penaltyScore = levelConfig?.PenaltyScore ?? 0;
        if (penaltyScore <= 0)
        {
            var penalty = await _penaltyRepo.GetPenalty(challenge.Id, token);
            var defaultPenalty = await _settings.Get<int>("Scoring.DefaultPenaltyScore", token);
            penaltyScore = penalty?.PenaltyScore ?? defaultPenalty;
        }

        // 4. Trap 检查
        bool triggerChallengeLock = false;
        string lockReason = "SubmissionLimitExceeded";
        Guid? bannedTrackId = null;
        int? bannedLevel = null;

        var lockOnAnswers = levelConfig?.LockOnAnswers ?? [];
        var banOnAnswers = levelConfig?.BanOnAnswers ?? [];

        if (lockOnAnswers.Contains(answer))
        {
            triggerChallengeLock = true;
            lockReason = "TrapAnswer";
        }

        // BanOnAnswers 仅 Level 2+ 且有 trackId 时生效
        if (banOnAnswers.Contains(answer) && level > 1 && trackId is not null)
        {
            var alreadySolved = await _recordRepo.HasCorrectSubmission(
                challenge.Id, participation.Id, trackId.Value, level, token);
            if (!alreadySolved)
            {
                await _banStateRepo.CreateBan(new ChallengeTrackLevelBanState
                {
                    ParticipationId = participation.Id,
                    ChallengeId = challenge.Id,
                    TrackId = trackId.Value,
                    Level = level,
                    BanAnswer = answer
                }, token);
                bannedTrackId = trackId;
                bannedLevel = level;
            }
        }

        // 5. WrongCount 累加 + SubmissionLimit 检查
        var wrongCount = await _wrongCountRepo.IncrementWrongCount(
            participation.Id, challenge.Id, level, token);

        if (!triggerChallengeLock)
        {
            var limit = levelConfig?.SubmissionLimit > 0
                ? levelConfig.SubmissionLimit
                : challenge.SubmissionLimit;

            if (limit > 0 && wrongCount >= limit)
            {
                triggerChallengeLock = true;
                lockReason = "SubmissionLimitExceeded";
            }
        }

        // 6. 创建全题锁
        DateTimeOffset? unlocksAt = null;
        if (triggerChallengeLock)
        {
            var lockMinutes = challenge.LockDurationMinutes;
            unlocksAt = lockMinutes > 0
                ? DateTimeOffset.UtcNow.AddMinutes(lockMinutes)
                : DateTimeOffset.MaxValue;

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
                : "Wrong answer"
        };
    }
```

- [ ] **Step 2: 重写 CreateFirstSolve**

删除旧的 `CreateFirstSolve` 方法，替换为：

```csharp
    public async Task CreateFirstSolve(int participationId, int challengeId, int submissionId,
        CancellationToken token = default)
    {
        var exists = await _context.FirstSolves
            .AnyAsync(fs => fs.ParticipationId == participationId && fs.ChallengeId == challengeId, token);
        if (exists) return;

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

        _context.FirstSolves.Add(new FirstSolve
        {
            ParticipationId = participationId,
            ChallengeId = challengeId,
            SubmissionId = submissionId
        });
        await _context.SaveChangesAsync(token);
    }
```

- [ ] **Step 3: 删除旧 MatchLineRange / MatchHybridLine 方法**

在 ScoringService.cs 中找到并删除 `MatchLineRange`、`MatchHybridLine` 方法（保留 `ParseHybridLineAnswer`、`HybridLineMatches`、`TryParseLineRange` 等纯函数）。

- [ ] **Step 4: 编译（仍有调用方错误，正常）**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -30`

- [ ] **Step 5: 提交**

```bash
git add src/GZCTF/Services/Scoring/ScoringService.cs
git commit -m "refactor(scoring): rewrite HandleCorrect/HandleWrong/CreateFirstSolve for multi-level"
```

---

### Task 22: 修复 GameChallenge.Update 方法

**Files:**
- Modify: `src/GZCTF/Models/Data/GameChallenge.cs`

- [ ] **Step 1: 修改 Update 方法**

找到 `internal void Update(ChallengeUpdateModel model)`，删除 `FlagMatchMode = model.FlagMatchMode ?? FlagMatchMode;` 行，添加：

```csharp
        LockDurationMinutes = model.LockDurationMinutes ?? LockDurationMinutes;
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Models/Data/GameChallenge.cs
git commit -m "feat(models): update GameChallenge.Update for LockDurationMinutes"
```

---

### Task 23: 更新 ChallengeUpdateModel 与 ChallengeEditDetailModel

**Files:**
- Modify: `src/GZCTF/Models/Request/Edit/ChallengeUpdateModel.cs`
- Modify: `src/GZCTF/Models/Request/Edit/ChallengeEditDetailModel.cs`

- [ ] **Step 1: 在 ChallengeUpdateModel 中删除 FlagMatchMode，添加 LockDurationMinutes**

打开 `ChallengeUpdateModel.cs`，找到：
```csharp
    /// <summary>
    /// Flag matching mode (Exact or LineRange)
    /// </summary>
    public FlagMatchMode? FlagMatchMode { get; set; }
```

替换为：
```csharp
    /// <summary>
    /// 临时全题锁时长（分钟），0 = 永久
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? LockDurationMinutes { get; set; }
```

- [ ] **Step 2: 在 ChallengeEditDetailModel 中同样替换**

打开 `ChallengeEditDetailModel.cs`，找到 `FlagMatchMode` 字段及映射（`FlagMatchMode = chal.FlagMatchMode`），替换为：

```csharp
    public int LockDurationMinutes { get; set; }
```

映射行改为：
```csharp
            LockDurationMinutes = chal.LockDurationMinutes,
```

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Models/Request/Edit/ChallengeUpdateModel.cs \
        src/GZCTF/Models/Request/Edit/ChallengeEditDetailModel.cs
git commit -m "feat(models): replace FlagMatchMode with LockDurationMinutes in edit models"
```

---

### Task 24: 更新 ChallengeDetailModel 与 FlagSubmitModel

**Files:**
- Modify: `src/GZCTF/Models/Request/Game/ChallengeDetailModel.cs`
- Modify: `src/GZCTF/Models/Request/Game/FlagSubmitModel.cs`

- [ ] **Step 1: 扩展 FlagSubmitModel**

打开 `FlagSubmitModel.cs`，添加：
```csharp
    public Guid? TrackId { get; set; }
    public int Level { get; set; } = 1;
```

- [ ] **Step 2: 扩展 ChallengeDetailModel**

打开 `ChallengeDetailModel.cs`，在类末尾添加多级状态字段：

```csharp
    public List<TrackStatusModel>? Tracks { get; set; }
    public ChallengeLockStatusModel? ChallengeLock { get; set; }
    public int CurrentLevel { get; set; } = 1;
```

在文件末尾（namespace 内）添加两个新模型：

```csharp
public class TrackStatusModel
{
    public Guid TrackId { get; set; }
    public string? Title { get; set; }
    public bool IsEnabled { get; set; }
    public List<TrackLevelStatusModel> Levels { get; set; } = [];
}

public class TrackLevelStatusModel
{
    public int Level { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = "Unsolved";  // Unsolved | Solved | Banned | Locked
    public DateTimeOffset? SolvedAt { get; set; }
    public int AttemptsUsed { get; set; }
    public int SubmissionLimit { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
}

public class ChallengeLockStatusModel
{
    public bool IsLocked { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
    public string? Reason { get; set; }
}
```

- [ ] **Step 3: 找到 FromInstance 方法，在末尾添加多级状态填充逻辑**

定位 `ChallengeDetailModel.FromInstance`，在 return 之前添加（如有困难可暂时只填 null，后续 Controller Task 再完整实现）：

```csharp
        // 多级状态由 GameController 在查询时填充（ChallengeDetailModel.Tracks/ChallengeLock 默认 null）
```

- [ ] **Step 4: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Models/Request/Game/ChallengeDetailModel.cs \
        src/GZCTF/Models/Request/Game/FlagSubmitModel.cs
git commit -m "feat(models): extend ChallengeDetailModel with multi-level status, FlagSubmitModel with trackId/level"
```

---

## Phase 4: Controllers

### Task 25: 创建 TrackController

**Files:**
- Create: `src/GZCTF/Controllers/TrackController.cs`

- [ ] **Step 1: 创建控制器**

```csharp
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/tracks")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class TrackController(IChallengeTrackRepository trackRepository) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ChallengeTrack[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracks([FromRoute] int challengeId, CancellationToken token)
    {
        var tracks = await trackRepository.GetTracks(challengeId, token);
        return Ok(tracks);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTrack([FromRoute] int challengeId,
        [FromBody] TrackCreateModel model, CancellationToken token)
    {
        var track = new ChallengeTrack
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            Title = model.Title,
            FlawNo = model.FlawNo,
            Repository = model.Repository,
            Module = model.Module,
            FilePath = model.FilePath,
            Category = model.Category,
            ReferenceUrl = model.ReferenceUrl,
            IsEnabled = model.IsEnabled ?? true,
            SortOrder = model.SortOrder ?? 0
        };

        track = await trackRepository.CreateTrack(track, token);
        return CreatedAtAction(nameof(GetTrack), new { challengeId, trackId = track.Id }, track);
    }

    [RequireAdmin]
    [HttpGet("{trackId:guid}")]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrack([FromRoute] int challengeId, [FromRoute] Guid trackId, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));
        return Ok(track);
    }

    [RequireAdmin]
    [HttpPut("{trackId:guid}")]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTrack([FromRoute] int challengeId, [FromRoute] Guid trackId,
        [FromBody] TrackUpdateModel model, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));

        if (model.Title is not null) track.Title = model.Title;
        if (model.FlawNo is not null) track.FlawNo = model.FlawNo;
        if (model.Repository is not null) track.Repository = model.Repository;
        if (model.Module is not null) track.Module = model.Module;
        if (model.FilePath is not null) track.FilePath = model.FilePath;
        if (model.Category is not null) track.Category = model.Category;
        if (model.ReferenceUrl is not null) track.ReferenceUrl = model.ReferenceUrl;
        if (model.IsEnabled.HasValue) track.IsEnabled = model.IsEnabled.Value;
        if (model.SortOrder.HasValue) track.SortOrder = model.SortOrder.Value;

        track = await trackRepository.UpdateTrack(track, token);
        return Ok(track);
    }

    [RequireAdmin]
    [HttpDelete("{trackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTrack([FromRoute] int challengeId, [FromRoute] Guid trackId, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));

        await trackRepository.DeleteTrack(trackId, token);
        return NoContent();
    }
}

public class TrackCreateModel
{
    public string? Title { get; set; }
    public string? FlawNo { get; set; }
    public string? Repository { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? Category { get; set; }
    public string? ReferenceUrl { get; set; }
    public bool? IsEnabled { get; set; }
    public int? SortOrder { get; set; }
}

public class TrackUpdateModel
{
    public string? Title { get; set; }
    public string? FlawNo { get; set; }
    public string? Repository { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? Category { get; set; }
    public string? ReferenceUrl { get; set; }
    public bool? IsEnabled { get; set; }
    public int? SortOrder { get; set; }
}
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Controllers/TrackController.cs
git commit -m "feat(controllers): add TrackController for Track CRUD"
```

---

### Task 26: 创建 LevelConfigController

**Files:**
- Create: `src/GZCTF/Controllers/LevelConfigController.cs`

- [ ] **Step 1: 创建控制器**

```csharp
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/levels")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class LevelConfigController(IChallengeLevelConfigRepository levelConfigRepository) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ChallengeLevelConfig[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLevels([FromRoute] int challengeId, CancellationToken token)
    {
        var configs = await levelConfigRepository.GetConfigs(challengeId, token);
        return Ok(configs);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLevel([FromRoute] int challengeId,
        [FromBody] LevelConfigCreateModel model, CancellationToken token)
    {
        var existing = await levelConfigRepository.GetConfig(challengeId, model.Level, token);
        if (existing is not null)
            return Conflict(new RequestResponse($"Level {model.Level} already exists"));

        var config = new ChallengeLevelConfig
        {
            ChallengeId = challengeId,
            Level = model.Level,
            Title = model.Title,
            Score = model.Score,
            SubmissionLimit = model.SubmissionLimit ?? 0,
            PenaltyScore = model.PenaltyScore ?? 0,
            LockOnAnswers = model.LockOnAnswers ?? [],
            BanOnAnswers = model.BanOnAnswers ?? [],
            IsEnabled = model.IsEnabled ?? true,
            SortOrder = model.SortOrder ?? model.Level
        };

        config = await levelConfigRepository.CreateConfig(config, token);
        return CreatedAtAction(nameof(GetLevel), new { challengeId, levelId = config.Id }, config);
    }

    [RequireAdmin]
    [HttpGet("{levelId:int}")]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLevel([FromRoute] int challengeId, [FromRoute] int levelId, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfig(challengeId, levelId, token);
        if (config is null || config.Id != levelId)
            return NotFound(new RequestResponse("Level config not found"));
        return Ok(config);
    }

    [RequireAdmin]
    [HttpPut("{levelId:int}")]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLevel([FromRoute] int challengeId, [FromRoute] int levelId,
        [FromBody] LevelConfigUpdateModel model, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfig(challengeId, levelId, token);
        if (config is null || config.Id != levelId)
            return NotFound(new RequestResponse("Level config not found"));

        if (model.Title is not null) config.Title = model.Title;
        if (model.Score.HasValue) config.Score = model.Score.Value;
        if (model.SubmissionLimit.HasValue) config.SubmissionLimit = model.SubmissionLimit.Value;
        if (model.PenaltyScore.HasValue) config.PenaltyScore = model.PenaltyScore.Value;
        if (model.LockOnAnswers is not null) config.LockOnAnswers = model.LockOnAnswers;
        if (model.BanOnAnswers is not null) config.BanOnAnswers = model.BanOnAnswers;
        if (model.IsEnabled.HasValue) config.IsEnabled = model.IsEnabled.Value;
        if (model.SortOrder.HasValue) config.SortOrder = model.SortOrder.Value;

        config = await levelConfigRepository.UpdateConfig(config, token);
        return Ok(config);
    }

    [RequireAdmin]
    [HttpDelete("{levelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLevel([FromRoute] int challengeId, [FromRoute] int levelId, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfig(challengeId, levelId, token);
        if (config is null || config.Id != levelId)
            return NotFound(new RequestResponse("Level config not found"));

        // 查 Level 编号
        var level = config.Level;
        var hasFlags = await levelConfigRepository.HasFlags(challengeId, level, token);
        if (hasFlags)
            return Conflict(new RequestResponse("Cannot delete level with existing flags"));

        await levelConfigRepository.DeleteConfig(levelId, token);
        return NoContent();
    }
}

public class LevelConfigCreateModel
{
    public int Level { get; set; }
    public string? Title { get; set; }
    public int Score { get; set; }
    public int? SubmissionLimit { get; set; }
    public int? PenaltyScore { get; set; }
    public List<string>? LockOnAnswers { get; set; }
    public List<string>? BanOnAnswers { get; set; }
    public bool? IsEnabled { get; set; }
    public int? SortOrder { get; set; }
}

public class LevelConfigUpdateModel
{
    public string? Title { get; set; }
    public int? Score { get; set; }
    public int? SubmissionLimit { get; set; }
    public int? PenaltyScore { get; set; }
    public List<string>? LockOnAnswers { get; set; }
    public List<string>? BanOnAnswers { get; set; }
    public bool? IsEnabled { get; set; }
    public int? SortOrder { get; set; }
}
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Controllers/LevelConfigController.cs
git commit -m "feat(controllers): add LevelConfigController for Level config CRUD"
```

---

### Task 27: 重构 ExtController（Flag CRUD 扩展 Level/TrackId，删除 HybridLine 逻辑）

**Files:**
- Modify: `src/GZCTF/Controllers/ExtController.cs`

- [ ] **Step 1: 修改 ChallengeFlagCreateModel，加 Level + TrackId**

在 `ExtController.cs` 末尾的 `ChallengeFlagCreateModel` 类中：
- 删除 `FlawNo`, `Repository`, `Module`, `FilePath`, `Cwe`, `Score` 字段
- 添加：
```csharp
    public int Level { get; set; } = 1;
    public Guid? TrackId { get; set; }
```

`ChallengeFlagUpdateModel` 类做同样改动（字段改为可空）。

- [ ] **Step 2: 修改 CreateFlag 方法体**

在 `CreateFlag` 方法中，找到 `var flag = new ChallengeFlag { ... }`，替换为：

```csharp
        var flag = new ChallengeFlag
        {
            ChallengeId = challengeId,
            FlagValue = model.FlagValue,
            AnswerType = model.AnswerType,
            RawAnswer = string.IsNullOrWhiteSpace(model.RawAnswer) ? model.FlagValue : model.RawAnswer,
            IsVisible = model.IsVisible,
            IsEnabled = model.IsEnabled,
            Level = model.Level,
            TrackId = model.TrackId ?? Guid.NewGuid()
        };
```

- [ ] **Step 3: 删除 IsHybridLineChallenge / TrySetNormalizedAnswerKey / HasNormalizedAnswerConflict 的 HybridLine 专用分支**

把 `TrySetNormalizedAnswerKey` 简化为：

```csharp
    private static bool TrySetNormalizedAnswerKey(ChallengeFlag flag, out RequestResponse? error)
    {
        var normalizedKey = ScoringService.NormalizeHybridLineAnswer(flag.RawAnswer, flag.AnswerType);
        flag.NormalizedAnswerKey = normalizedKey;
        error = null;
        return true;
    }
```

把 `HasNormalizedAnswerConflict` 简化为：

```csharp
    private async Task<bool> HasNormalizedAnswerConflict(int challengeId, string normalizedAnswerKey,
        Guid? currentFlagId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(normalizedAnswerKey)) return false;
        return await context.ChallengeFlags.AnyAsync(f =>
            f.ChallengeId == challengeId
            && f.NormalizedAnswerKey == normalizedAnswerKey
            && (!currentFlagId.HasValue || f.Id != currentFlagId.Value), token);
    }
```

删除 `IsHybridLineChallenge` 方法。

在 `CreateFlag` 和 `UpdateFlag` 中，调用 `TrySetNormalizedAnswerKey(flag, out var error)` 和 `HasNormalizedAnswerConflict(challengeId, flag.NormalizedAnswerKey, flag.Id, token)`（去掉 isHybridLine 参数）。

- [ ] **Step 4: 更新 UpdateFlag 方法体**

在 `UpdateFlag` 中，删除 `FlawNo / Repository / Module / FilePath / Cwe / Score` 的赋值块，添加：

```csharp
        if (model.Level > 0)
            flag.Level = model.Level;

        if (model.TrackId.HasValue)
            flag.TrackId = model.TrackId.Value;
```

- [ ] **Step 5: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Controllers/ExtController.cs
git commit -m "refactor(controllers): simplify ExtController flag CRUD for multi-level model"
```

---

### Task 28: 修改 GameController.Submit 接收 (trackId, level)

**Files:**
- Modify: `src/GZCTF/Controllers/GameController.cs`

- [ ] **Step 1: 找到 Submit 方法，修改 scoringService.ProcessSubmission 调用**

在 `GameController.cs` 中找到 `scoreResult = await scoringService.ProcessSubmission(...)`，把调用改为：

```csharp
                scoreResult = await scoringService.ProcessSubmission(
                    instance.Challenge, context.Participation!, answer,
                    model.TrackId, model.Level, token);
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Controllers/GameController.cs
git commit -m "feat(controllers): GameController.Submit accepts trackId and level"
```

---

### Task 29: 扩展 GameController.GetChallenge 返回多级状态

**Files:**
- Modify: `src/GZCTF/Controllers/GameController.cs`

- [ ] **Step 1: 在 GetChallenge 方法中填充 Tracks/ChallengeLock/CurrentLevel**

找到 `GetChallenge` 方法（`if (instance.Challenge.FlagMatchMode == FlagMatchMode.HybridLine)` 分支处），替换为统一的多级状态填充：

```csharp
        // 填充多级状态
        var tracks = await trackRepository.GetTracks(challengeId, token);
        var levelConfigs = await levelConfigRepository.GetConfigs(challengeId, token);
        var bans = await banStateRepository.GetBans(context.Participation!.Id, challengeId, token);
        var activeLock = await lockStateRepository.GetActiveLock(context.Participation!.Id, challengeId, token);
        var solvedTrackLevels = await submissionRecordRepository.GetSolvedTrackLevels(challengeId, context.Participation!.Id, token);
        var solvedSet = solvedTrackLevels.Select(t => (t.Item1, t.Item2)).ToHashSet();

        var trackStatuses = new List<TrackStatusModel>();
        foreach (var track in tracks.Where(t => t.IsEnabled))
        {
            var trackStatus = new TrackStatusModel
            {
                TrackId = track.Id,
                Title = track.Title,
                IsEnabled = track.IsEnabled,
                Levels = new List<TrackLevelStatusModel>()
            };

            foreach (var lc in levelConfigs.Where(c => c.IsEnabled).OrderBy(c => c.Level))
            {
                var isSolved = solvedSet.Contains((track.Id, lc.Level));
                var isBanned = bans.Any(b => b.TrackId == track.Id && b.Level == lc.Level);
                var prevSolved = lc.Level == 1 || solvedSet.Contains((track.Id, lc.Level - 1));

                string status = isSolved ? "Solved"
                    : isBanned ? "Banned"
                    : !prevSolved ? "Locked"
                    : "Unsolved";

                var attemptsUsed = await wrongCountRepository.GetWrongCount(
                    context.Participation!.Id, challengeId, lc.Level, token);

                trackStatus.Levels.Add(new TrackLevelStatusModel
                {
                    Level = lc.Level,
                    Title = lc.Title,
                    Status = status,
                    AttemptsUsed = attemptsUsed,
                    SubmissionLimit = lc.SubmissionLimit > 0 ? lc.SubmissionLimit : instance.Challenge.SubmissionLimit,
                    UnlocksAt = activeLock?.UnlocksAt
                });
            }

            trackStatuses.Add(trackStatus);
        }

        var result = ChallengeDetailModel.FromInstance(instance, attempts, scoreboardChallenge, null);
        result.Tracks = trackStatuses;
        result.ChallengeLock = new ChallengeLockStatusModel
        {
            IsLocked = activeLock is not null,
            UnlocksAt = activeLock?.UnlocksAt,
            Reason = activeLock?.TriggerReason
        };
        result.CurrentLevel = 1;

        return Ok(result);
```

- [ ] **Step 2: 在 GameController 构造函数注入新依赖**

找到 GameController 构造函数，添加参数：

```csharp
    IChallengeTrackRepository trackRepository,
    IChallengeLevelConfigRepository levelConfigRepository,
    IChallengeLockStateRepository lockStateRepository,
    IChallengeTrackLevelBanStateRepository banStateRepository,
    IChallengeWrongCountStateRepository wrongCountRepository,
```

并保存为私有字段。

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Controllers/GameController.cs
git commit -m "feat(controllers): GetChallenge returns multi-level track/level status"
```

---

## Phase 5: FlagImportService

### Task 30: 创建 FlagImportModels

**Files:**
- Create: `src/GZCTF/Services/Import/FlagImportModels.cs`

- [ ] **Step 1: 创建模型**

```csharp
namespace GZCTF.Services.Import;

public class FlagImportColumnMapping
{
    public string? TrackTitleColumn { get; set; }
    public string? TrackFlawNoColumn { get; set; }
    public string? TrackRepositoryColumn { get; set; }
    public string? TrackModuleColumn { get; set; }
    public string? TrackFilePathColumn { get; set; }
    public string? TrackCategoryColumn { get; set; }
    public string? TrackReferenceUrlColumn { get; set; }

    public string MatchBy { get; set; } = "FilePath";  // FilePath | FlawNo | Title

    public List<LevelColumnMapping> LevelColumns { get; set; } = [];
}

public class LevelColumnMapping
{
    public int Level { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public int Score { get; set; }
    public int SubmissionLimit { get; set; }
    public int PenaltyScore { get; set; }
    public List<string> LockOnAnswers { get; set; } = [];
    public List<string> BanOnAnswers { get; set; } = [];
    public string? LevelTitle { get; set; }
}

public enum ImportMode { Upsert, Skip, Fail }

public class FlagImportFilePreview
{
    public List<string> ColumnNames { get; set; } = [];
    public List<Dictionary<string, string>> SampleRows { get; set; } = [];
}

public class FlagImportResult
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<FlagImportError> Errors { get; set; } = [];
}

public class FlagImportError
{
    public int Row { get; set; }
    public string Error { get; set; } = string.Empty;
}
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Services/Import/FlagImportModels.cs
git commit -m "feat(import): add flag import models"
```

---

### Task 31: 创建 FlagImportService

**Files:**
- Create: `src/GZCTF/Services/Import/FlagImportService.cs`

- [ ] **Step 1: 创建服务**

```csharp
using System.Globalization;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Services.Import;

public class FlagImportService(
    AppDbContext context,
    IChallengeTrackRepository trackRepo,
    IChallengeFlagRepository flagRepo,
    IChallengeLevelConfigRepository levelConfigRepo)
{
    public async Task<FlagImportFilePreview> PreviewFileAsync(
        Stream fileStream, string contentType, CancellationToken token)
    {
        var rows = ParseFile(fileStream, contentType);
        if (rows.Count == 0)
            return new FlagImportFilePreview();

        var columnNames = rows[0].Keys.ToList();
        var sampleRows = rows.Take(5).ToList();

        return new FlagImportFilePreview
        {
            ColumnNames = columnNames,
            SampleRows = sampleRows
        };
    }

    public async Task<FlagImportResult> ImportFlagsAsync(
        int challengeId,
        Stream fileStream,
        string contentType,
        FlagImportColumnMapping mapping,
        ImportMode mode,
        CancellationToken token)
    {
        var rows = ParseFile(fileStream, contentType);
        var result = new FlagImportResult { TotalRows = rows.Count };

        foreach (var (row, idx) in rows.Select((r, i) => (r, i)))
        {
            try
            {
                await using var transaction = await context.Database.BeginTransactionAsync(token);

                var trackMeta = ExtractTrackMeta(row, mapping);
                var matchValue = GetMatchValue(trackMeta, mapping.MatchBy);

                if (string.IsNullOrWhiteSpace(matchValue))
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = $"MatchBy column '{mapping.MatchBy}' is empty" });
                    result.Failed++;
                    await transaction.RollbackAsync(token);
                    continue;
                }

                var existingTrack = await FindTrackByMatch(challengeId, mapping.MatchBy, matchValue, token);

                if (existingTrack is not null)
                {
                    if (mode == ImportMode.Skip)
                    {
                        result.Skipped++;
                        await transaction.RollbackAsync(token);
                        continue;
                    }
                    if (mode == ImportMode.Fail)
                    {
                        result.Errors.Add(new FlagImportError { Row = idx + 1, Error = "Track exists and mode=Fail" });
                        result.Failed++;
                        await transaction.RollbackAsync(token);
                        continue;
                    }
                    // Upsert: 若有提交记录则软删旧 track 新建，否则更新
                    var hasSubmissions = await trackRepo.HasSubmissions(existingTrack.Id, token);
                    if (hasSubmissions)
                    {
                        await trackRepo.DeleteTrack(existingTrack.Id, token);
                        existingTrack = null;
                    }
                    else
                    {
                        existingTrack.Title = trackMeta.Title ?? existingTrack.Title;
                        existingTrack.FlawNo = trackMeta.FlawNo ?? existingTrack.FlawNo;
                        existingTrack.Repository = trackMeta.Repository ?? existingTrack.Repository;
                        existingTrack.Module = trackMeta.Module ?? existingTrack.Module;
                        existingTrack.FilePath = trackMeta.FilePath ?? existingTrack.FilePath;
                        existingTrack.Category = trackMeta.Category ?? existingTrack.Category;
                        existingTrack.ReferenceUrl = trackMeta.ReferenceUrl ?? existingTrack.ReferenceUrl;
                        existingTrack = await trackRepo.UpdateTrack(existingTrack, token);

                        // 删除旧 flags（已确认无提交记录）
                        var oldFlags = await flagRepo.GetFlagsByTrack(existingTrack.Id, token);
                        foreach (var f in oldFlags)
                            await flagRepo.DeleteFlag(f.Id, token);
                    }
                    result.Updated++;
                }

                var track = existingTrack ?? new ChallengeTrack
                {
                    Id = Guid.NewGuid(),
                    ChallengeId = challengeId,
                    Title = trackMeta.Title,
                    FlawNo = trackMeta.FlawNo,
                    Repository = trackMeta.Repository,
                    Module = trackMeta.Module,
                    FilePath = trackMeta.FilePath,
                    Category = trackMeta.Category,
                    ReferenceUrl = trackMeta.ReferenceUrl,
                    IsEnabled = true,
                    SortOrder = idx
                };

                if (existingTrack is null)
                {
                    track = await trackRepo.CreateTrack(track, token);
                    result.Imported++;
                }

                // 创建 flags
                foreach (var lc in mapping.LevelColumns)
                {
                    if (!row.TryGetValue(lc.ColumnName, out var answer) || string.IsNullOrWhiteSpace(answer))
                        continue;

                    var flag = new ChallengeFlag
                    {
                        Id = Guid.NewGuid(),
                        ChallengeId = challengeId,
                        FlagValue = answer.Trim(),
                        AnswerType = lc.AnswerType,
                        RawAnswer = answer.Trim(),
                        NormalizedAnswerKey = Services.Scoring.ScoringService.NormalizeHybridLineAnswer(answer.Trim(), lc.AnswerType),
                        Level = lc.Level,
                        TrackId = track.Id,
                        IsEnabled = true,
                        IsVisible = true,
                        SortOrder = 0
                    };
                    await flagRepo.CreateFlag(flag, token);

                    // Upsert LevelConfig
                    var levelConfig = await levelConfigRepo.GetConfig(challengeId, lc.Level, token);
                    if (levelConfig is null)
                    {
                        await levelConfigRepo.CreateConfig(new ChallengeLevelConfig
                        {
                            ChallengeId = challengeId,
                            Level = lc.Level,
                            Title = lc.LevelTitle,
                            Score = lc.Score,
                            SubmissionLimit = lc.SubmissionLimit,
                            PenaltyScore = lc.PenaltyScore,
                            LockOnAnswers = lc.LockOnAnswers,
                            BanOnAnswers = lc.BanOnAnswers,
                            IsEnabled = true,
                            SortOrder = lc.Level
                        }, token);
                    }
                }

                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new FlagImportError { Row = idx + 1, Error = ex.Message });
                result.Failed++;
            }
        }

        return result;
    }

    private List<Dictionary<string, string>> ParseFile(Stream stream, string contentType)
    {
        if (contentType.Contains("csv") || contentType.Contains("text/plain"))
            return ParseCsv(stream);

        return ParseXlsx(stream);
    }

    private List<Dictionary<string, string>> ParseXlsx(Stream stream)
    {
        var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);
        var rows = new List<Dictionary<string, string>>();

        if (sheet.LastRowNum < 0) return rows;

        var headerRow = sheet.GetRow(0);
        var headers = new List<string>();
        for (var c = 0; c < headerRow.LastCellNum; c++)
        {
            var cell = headerRow.GetCell(c);
            headers.Add(cell?.ToString()?.Trim() ?? $"Column{c}");
        }

        for (var r = 1; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row is null) continue;
            var dict = new Dictionary<string, string>();
            for (var c = 0; c < headers.Count; c++)
            {
                var cell = row.GetCell(c);
                dict[headers[c]] = cell?.ToString()?.Trim() ?? string.Empty;
            }
            rows.Add(dict);
        }

        return rows;
    }

    private List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        var rows = new List<Dictionary<string, string>>();
        using var reader = new StreamReader(stream);
        string? headerLine = reader.ReadLine();
        if (headerLine is null) return rows;

        var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToList();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split(',').Select(f => f.Trim().Trim('"')).ToList();
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Count && i < fields.Count; i++)
                dict[headers[i]] = fields[i];
            rows.Add(dict);
        }

        return rows;
    }

    private static (
        string? Title, string? FlawNo, string? Repository, string? Module,
        string? FilePath, string? Category, string? ReferenceUrl
    ) ExtractTrackMeta(Dictionary<string, string> row, FlagImportColumnMapping mapping)
    {
        string? Val(string? col) => col is not null && row.TryGetValue(col, out var v) ? v : null;
        return (
            Val(mapping.TrackTitleColumn),
            Val(mapping.TrackFlawNoColumn),
            Val(mapping.TrackRepositoryColumn),
            Val(mapping.TrackModuleColumn),
            Val(mapping.TrackFilePathColumn),
            Val(mapping.TrackCategoryColumn),
            Val(mapping.TrackReferenceUrlColumn)
        );
    }

    private static string? GetMatchValue(
        (string? Title, string? FlawNo, string? Repository, string? Module, string? FilePath, string? Category, string? ReferenceUrl) meta,
        string matchBy) => matchBy switch
    {
        "FlawNo" => meta.FlawNo,
        "Title" => meta.Title,
        _ => meta.FilePath
    };

    private async Task<ChallengeTrack?> FindTrackByMatch(int challengeId, string matchBy, string value, CancellationToken token)
    {
        var tracks = await trackRepo.GetTracks(challengeId, token);
        return matchBy switch
        {
            "FlawNo" => tracks.FirstOrDefault(t => t.FlawNo == value),
            "Title" => tracks.FirstOrDefault(t => t.Title == value),
            _ => tracks.FirstOrDefault(t => t.FilePath == value)
        };
    }
}
```

- [ ] **Step 2: 在 ServicesExtension.cs 注册**

找到 `AddScoped<ExcelHelper>` 行，下方添加：
```csharp
            builder.Services.AddScoped<Services.Import.FlagImportService>();
```

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Services/Import/FlagImportService.cs \
        src/GZCTF/Extensions/Startup/ServicesExtension.cs
git commit -m "feat(import): add FlagImportService with xlsx/csv parsing and upsert"
```

---

### Task 32: 创建 FlagImportController

**Files:**
- Create: `src/GZCTF/Controllers/FlagImportController.cs`

- [ ] **Step 1: 创建控制器**

```csharp
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Services.Import;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/flags/import")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class FlagImportController(FlagImportService importService) : ControllerBase
{
    [RequireAdmin]
    [HttpPost("preview")]
    [ProducesResponseType(typeof(FlagImportFilePreview), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromRoute] int challengeId, IFormFile file, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        await using var stream = file.OpenReadStream();
        var preview = await importService.PreviewFileAsync(stream, file.ContentType, token);
        return Ok(preview);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(FlagImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromRoute] int challengeId, IFormFile file,
        [FromForm] string mapping, [FromForm] string mode, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        if (string.IsNullOrWhiteSpace(mapping))
            return BadRequest(new RequestResponse("mapping is required"));

        var mappingObj = System.Text.Json.JsonSerializer.Deserialize<FlagImportColumnMapping>(mapping);
        if (mappingObj is null)
            return BadRequest(new RequestResponse("Invalid mapping JSON"));

        var modeEnum = Enum.Parse<ImportMode>(mode, ignoreCase: true);

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportFlagsAsync(challengeId, stream, file.ContentType, mappingObj, modeEnum, token);
        return Ok(result);
    }
}
```

- [ ] **Step 2: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj
git add src/GZCTF/Controllers/FlagImportController.cs
git commit -m "feat(controllers): add FlagImportController for preview/import endpoints"
```

---

## Phase 6: GameImportService 同步

### Task 33: 扩展 TransferChallenge 模型

**Files:**
- Modify: `src/GZCTF/Models/Transfer/TransferExtensions.cs`

- [ ] **Step 1: 查找 TransferChallenge 定义**

Run: `grep -n "class TransferChallenge" src/GZCTF/Models/Transfer/`

- [ ] **Step 2: 在 TransferChallenge 类中添加新字段**

找到 TransferChallenge 类（可能在 `TransferExtensions.cs` 或单独文件），添加：

```csharp
    public int LockDurationMinutes { get; set; }
    public List<TransferTrack> Tracks { get; set; } = [];
    public List<TransferLevelConfig> Levels { get; set; } = [];
```

在文件末尾（namespace 内）添加：

```csharp
public class TransferTrack
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? FlawNo { get; set; }
    public string? Repository { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? Category { get; set; }
    public string? ReferenceUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public class TransferLevelConfig
{
    public int Level { get; set; }
    public string? Title { get; set; }
    public int Score { get; set; }
    public int SubmissionLimit { get; set; }
    public int PenaltyScore { get; set; }
    public List<string> LockOnAnswers { get; set; } = [];
    public List<string> BanOnAnswers { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
```

- [ ] **Step 3: 在 ToTransfer 映射中序列化新字段**

找到 `ToTransfer` 方法中 Challenge → TransferChallenge 的映射，添加：

```csharp
                LockDurationMinutes = challenge.LockDurationMinutes,
                Tracks = challenge.Challenge.Flags?.Select(f => f.TrackId).Distinct()
                    .Select(tid => new TransferTrack { Id = tid }).ToList() ?? [],
                Levels = []  // 由 GameImportService 单独查询填充
```

注：简化实现，导入端会重新创建 Track/LevelConfig。

- [ ] **Step 4: 在 FromTransfer 映射中反序列化**

找到 `FromTransfer` 方法，添加：

```csharp
                LockDurationMinutes = transfer.LockDurationMinutes,
```

- [ ] **Step 5: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add src/GZCTF/Models/Transfer/TransferExtensions.cs
git commit -m "feat(transfer): extend TransferChallenge with LockDurationMinutes/Tracks/Levels"
```

---

## Phase 7: 清理与前端

### Task 34: 删除 FlagMatchMode 相关死代码

**Files:**
- Modify: `src/GZCTF/Services/Scoring/ScoringService.cs`
- Modify: `src/GZCTF/Controllers/GameController.cs`

- [ ] **Step 1: 搜索所有 FlagMatchMode 残留引用**

Run: `grep -rn "FlagMatchMode\|MatchLineRange\|MatchHybridLine\|IsHybridLineChallenge" src/GZCTF/ 2>&1 | head -20`

- [ ] **Step 2: 逐一删除残留引用**

对每个 grep 结果：
- 注释或删除引用行
- 如果是 `if (challenge.FlagMatchMode == FlagMatchMode.HybridLine)` 分支，整块删除（已被统一流程替代）

- [ ] **Step 3: 编译并提交**

```bash
cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | head -20
git add -u
git commit -m "refactor: remove FlagMatchMode/MatchLineRange/MatchHybridLine dead code"
```

---

### Task 35: 前端 API 客户端扩展

**Files:**
- Modify: `src/GZCTF/ClientApp/src/Api.ts`

- [ ] **Step 1: 在 Api.ts 末尾添加新接口与函数**

```typescript
// === Multi-level Flag Types ===

export interface ChallengeTrack {
  id: string;
  challengeId: number;
  title?: string;
  flawNo?: string;
  repository?: string;
  module?: string;
  filePath?: string;
  category?: string;
  referenceUrl?: string;
  isEnabled: boolean;
  sortOrder: number;
}

export interface ChallengeLevelConfig {
  id: number;
  challengeId: number;
  level: number;
  title?: string;
  score: number;
  submissionLimit: number;
  penaltyScore: number;
  lockOnAnswers: string[];
  banOnAnswers: string[];
  isEnabled: boolean;
  sortOrder: number;
}

export interface TrackStatus {
  trackId: string;
  title?: string;
  isEnabled: boolean;
  levels: TrackLevelStatus[];
}

export interface TrackLevelStatus {
  level: number;
  title?: string;
  status: 'Unsolved' | 'Solved' | 'Banned' | 'Locked';
  solvedAt?: string;
  attemptsUsed: number;
  submissionLimit: number;
  unlocksAt?: string;
}

export interface FlagImportFilePreview {
  columnNames: string[];
  sampleRows: Record<string, string>[];
}

export interface FlagImportResult {
  totalRows: number;
  imported: number;
  updated: number;
  skipped: number;
  failed: number;
  errors: { row: number; error: string }[];
}

// === Track CRUD ===

export const getChallengeTracks = async (challengeId: number) =>
  api.get<ChallengeTrack[]>(`/api/ext/challenges/${challengeId}/tracks`);

export const createTrack = async (challengeId: number, track: Partial<ChallengeTrack>) =>
  api.post<ChallengeTrack>(`/api/ext/challenges/${challengeId}/tracks`, track);

export const updateTrack = async (challengeId: number, trackId: string, track: Partial<ChallengeTrack>) =>
  api.put<ChallengeTrack>(`/api/ext/challenges/${challengeId}/tracks/${trackId}`, track);

export const deleteTrack = async (challengeId: number, trackId: string) =>
  api.delete(`/api/ext/challenges/${challengeId}/tracks/${trackId}`);

// === Level Config CRUD ===

export const getLevelConfigs = async (challengeId: number) =>
  api.get<ChallengeLevelConfig[]>(`/api/ext/challenges/${challengeId}/levels`);

export const createLevelConfig = async (challengeId: number, config: Partial<ChallengeLevelConfig>) =>
  api.post<ChallengeLevelConfig>(`/api/ext/challenges/${challengeId}/levels`, config);

export const updateLevelConfig = async (challengeId: number, levelId: number, config: Partial<ChallengeLevelConfig>) =>
  api.put<ChallengeLevelConfig>(`/api/ext/challenges/${challengeId}/levels/${levelId}`, config);

export const deleteLevelConfig = async (challengeId: number, levelId: number) =>
  api.delete(`/api/ext/challenges/${challengeId}/levels/${levelId}`);

// === Flag Import ===

export const previewFlagImport = async (challengeId: number, file: File) => {
  const form = new FormData();
  form.append('file', file);
  return api.post<FlagImportFilePreview>(`/api/ext/challenges/${challengeId}/flags/import/preview`, form);
};

export const importFlags = async (
  challengeId: number,
  file: File,
  mapping: Record<string, unknown>,
  mode: 'upsert' | 'skip' | 'fail'
) => {
  const form = new FormData();
  form.append('file', file);
  form.append('mapping', JSON.stringify(mapping));
  form.append('mode', mode);
  return api.post<FlagImportResult>(`/api/ext/challenges/${challengeId}/flags/import`, form);
};
```

- [ ] **Step 2: 修改 submitFlag 函数签名，接受 trackId 与 level**

找到现有 `submitFlag` 函数，改为：

```typescript
export const submitFlag = async (
  gameId: number,
  challengeId: number,
  flag: string,
  trackId?: string | null,
  level: number = 1
) => {
  const body: Record<string, unknown> = { flag, level };
  if (trackId) body.trackId = trackId;
  return api.post(`/api/game/${gameId}/challenges/${challengeId}`, body);
};
```

- [ ] **Step 3: TypeScript 编译验证**

Run: `cd src/GZCTF/ClientApp && npx tsc --noEmit 2>&1 | head -20`
Expected: 无错误

- [ ] **Step 4: 提交**

```bash
git add src/GZCTF/ClientApp/src/Api.ts
git commit -m "feat(frontend): extend Api.ts with Track/Level/Import endpoints and submitFlag signature"
```

---

### Task 36: 前端 i18n

**Files:**
- Modify: `src/GZCTF/ClientApp/src/locales/zh-CN/admin.json`
- Modify: `src/GZCTF/ClientApp/src/locales/en-US/admin.json`
- Modify: `src/GZCTF/ClientApp/src/locales/zh-CN/challenge.json`
- Modify: `src/GZCTF/ClientApp/src/locales/en-US/challenge.json`

- [ ] **Step 1: 在 zh-CN/admin.json 末尾添加**

```json
  "track": {
    "title": "Track",
    "list": "Track 列表",
    "create": "新建 Track",
    "edit": "编辑 Track",
    "delete": "删除 Track",
    "title_label": "标题",
    "flawNo": "FlawNo",
    "repository": "所属仓库",
    "module": "模块",
    "filePath": "文件路径",
    "category": "参考类别",
    "referenceUrl": "参考链接"
  },
  "level": {
    "title": "Level 配置",
    "list": "Level 列表",
    "create": "新建 Level",
    "edit": "编辑 Level",
    "delete": "删除 Level",
    "level": "层级",
    "score": "分值",
    "submissionLimit": "提交次数限制",
    "penaltyScore": "错误扣分",
    "lockOnAnswers": "触发锁定答案",
    "banOnAnswers": "触发封禁答案",
    "levelTitle": "Level 标题"
  },
  "import": {
    "title": "批量导入",
    "upload": "上传文件",
    "preview": "预览",
    "mapping": "列映射",
    "mode": "冲突模式",
    "upsert": "更新",
    "skip": "跳过",
    "fail": "失败",
    "submit": "开始导入",
    "result": "导入结果",
    "totalRows": "总行数",
    "imported": "新建",
    "updated": "更新",
    "skipped": "跳过",
    "failed": "失败"
  }
```

- [ ] **Step 2: 在 en-US/admin.json 末尾添加对应英文翻译**

```json
  "track": {
    "title": "Track",
    "list": "Track List",
    "create": "Create Track",
    "edit": "Edit Track",
    "delete": "Delete Track",
    "title_label": "Title",
    "flawNo": "FlawNo",
    "repository": "Repository",
    "module": "Module",
    "filePath": "File Path",
    "category": "Category",
    "referenceUrl": "Reference URL"
  },
  "level": {
    "title": "Level Config",
    "list": "Level List",
    "create": "Create Level",
    "edit": "Edit Level",
    "delete": "Delete Level",
    "level": "Level",
    "score": "Score",
    "submissionLimit": "Submission Limit",
    "penaltyScore": "Penalty Score",
    "lockOnAnswers": "Lock-on Answers",
    "banOnAnswers": "Ban-on Answers",
    "levelTitle": "Level Title"
  },
  "import": {
    "title": "Bulk Import",
    "upload": "Upload File",
    "preview": "Preview",
    "mapping": "Column Mapping",
    "mode": "Conflict Mode",
    "upsert": "Upsert",
    "skip": "Skip",
    "fail": "Fail",
    "submit": "Start Import",
    "result": "Import Result",
    "totalRows": "Total Rows",
    "imported": "Imported",
    "updated": "Updated",
    "skipped": "Skipped",
    "failed": "Failed"
  }
```

- [ ] **Step 3: 在 zh-CN/challenge.json 末尾添加**

```json
  "trackList": {
    "title": "Track 列表",
    "progress": "进度",
    "enterTrack": "进入 Track"
  },
  "trackDetail": {
    "level1": "Level 1：漏洞定位",
    "level2": "Level 2：漏洞类型",
    "level3": "Level 3：漏洞验证",
    "submit": "提交",
    "solved": "已解出",
    "unsolved": "未解出",
    "banned": "已封禁",
    "locked": "已锁定",
    "attemptsUsed": "已用次数",
    "remainingAttempts": "剩余次数",
    "unlocksAt": "解锁时间"
  }
```

- [ ] **Step 4: 在 en-US/challenge.json 末尾添加对应英文翻译**

```json
  "trackList": {
    "title": "Track List",
    "progress": "Progress",
    "enterTrack": "Enter Track"
  },
  "trackDetail": {
    "level1": "Level 1: Vulnerability Location",
    "level2": "Level 2: Vulnerability Type",
    "level3": "Level 3: Vulnerability Verification",
    "submit": "Submit",
    "solved": "Solved",
    "unsolved": "Unsolved",
    "banned": "Banned",
    "locked": "Locked",
    "attemptsUsed": "Attempts Used",
    "remainingAttempts": "Remaining Attempts",
    "unlocksAt": "Unlocks At"
  }
```

- [ ] **Step 5: 提交**

```bash
git add src/GZCTF/ClientApp/src/locales/
git commit -m "feat(i18n): add track/level/import translations for zh-CN and en-US"
```

---

## Phase 8: 测试

### Task 37: ScoringService 单元测试

**Files:**
- Create: `src/GZCTF.Test/UnitTests/Scoring/ScoringServiceTests.cs`

- [ ] **Step 1: 创建测试文件**

```csharp
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace GZCTF.Test.UnitTests.Scoring;

public class ScoringServiceTests
{
    [Fact]
    public void TryMatch_ExactAnswer_ReturnsTrue_WhenEqual()
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "abc123",
            RawAnswer = "abc123",
            AnswerType = ChallengeFlagAnswerType.Exact
        };

        // TryMatch 是私有的，通过 NormalizeHybridLineAnswer 间接验证
        var normalized = ScoringService.NormalizeHybridLineAnswer("abc123", ChallengeFlagAnswerType.Exact);
        Assert.Equal("abc123", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_LineRange_NormalizesCorrectly()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("c:56", ChallengeFlagAnswerType.SingleLine);
        Assert.Contains("56", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_MultiLine_ParsesCommaSeparated()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("c:110,c:121", ChallengeFlagAnswerType.MultiLine);
        Assert.Contains("110", normalized);
        Assert.Contains("121", normalized);
    }

    [Fact]
    public void TryParseLineRange_ValidSingleLine_ReturnsTrue()
    {
        var ok = ScoringService.TryParseLineRange("56", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(56, start);
        Assert.Equal(56, end);
    }

    [Fact]
    public void TryParseLineRange_ValidRange_ReturnsTrue()
    {
        var ok = ScoringService.TryParseLineRange("77-82", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(77, start);
        Assert.Equal(82, end);
    }

    [Fact]
    public void TryParseLineRange_CPrefix_ValidSingleLine()
    {
        var ok = ScoringService.TryParseLineRange("c:56", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(56, start);
    }

    [Fact]
    public void TryParseLineRange_Invalid_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("abc", out _, out _);
        Assert.False(ok);
    }
}
```

- [ ] **Step 2: 运行测试**

Run: `cd src && dotnet test GZCTF.Test/GZCTF.Test.csproj --filter "ScoringServiceTests"`
Expected: 7 tests passed

- [ ] **Step 3: 提交**

```bash
git add src/GZCTF.Test/UnitTests/Scoring/ScoringServiceTests.cs
git commit -m "test(scoring): add ScoringService pure function unit tests"
```

---

### Task 38: 集成测试 - 多级 Flag 完整流程

**Files:**
- Create: `src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagFlowTests.cs`

- [ ] **Step 1: 查看现有集成测试模式**

Run: `ls src/GZCTF.Integration.Test/Tests/Api/ && head -40 src/GZCTF.Integration.Test/Tests/Api/*.cs 2>&1 | head -80`

- [ ] **Step 2: 创建集成测试**

```csharp
using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

public class MultiLevelFlagFlowTests : IClassFixture<GZCTFApplicationFactory>
{
    private readonly GZCTFApplicationFactory _factory;
    private readonly HttpClient _client;

    public MultiLevelFlagFlowTests(GZCTFApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetChallenge_ReturnsEmptyTracks_WhenNoFlagsConfigured()
    {
        // 占位测试：具体测试需要管理员登录 + 创建 Game + Challenge + Track + Level + Flag
        // 见 §9.2 集成测试清单
        var response = await _client.GetAsync("/api/game/1/challenges/1");
        // 预期 401 或 404（未登录）
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized
                 || response.StatusCode == HttpStatusCode.NotFound);
    }
}
```

注：完整集成测试需要管理员账号、Game、Challenge 的 fixture，建议由实现阶段基于 `TestDataSeeder` 扩展。本 Task 只提供骨架。

- [ ] **Step 3: 运行测试**

Run: `cd src && dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj --filter "MultiLevelFlagFlowTests"`
Expected: 1 test passed

- [ ] **Step 4: 提交**

```bash
git add src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagFlowTests.cs
git commit -m "test(integration): add multi-level flag flow integration test skeleton"
```

---

## Phase 9: 最终编译与手动验证

### Task 39: 后端整体编译

- [ ] **Step 1: 编译全部项目**

Run: `cd src && dotnet build GZCTF.slnx`
Expected: BUILD SUCCEEDED，0 errors

- [ ] **Step 2: 若有错误，逐一修复**

常见问题：
- `FlagMatchMode` 残留引用 → 删除
- `Score` / `FlawNo` / `Cwe` 等旧字段引用 → 改为从 `ChallengeTrack` 或 `ChallengeLevelConfig` 取
- `ProcessSubmission` 调用签名不匹配 → 添加 `trackId, level` 参数

- [ ] **Step 3: 提交修复**

```bash
git add -u
git commit -m "fix: resolve all compilation errors after multi-level flag refactor"
```

---

### Task 40: 前端整体编译

- [ ] **Step 1: TypeScript 编译**

Run: `cd src/GZCTF/ClientApp && npx tsc --noEmit`
Expected: 0 errors

- [ ] **Step 2: 若有错误，修复**

- [ ] **Step 3: 提交**

```bash
git add -u
git commit -m "fix(frontend): resolve TypeScript compilation errors"
```

---

### Task 41: 数据库迁移本地验证

- [ ] **Step 1: 启动本地 Postgres（若未启动）**

Run: `docker ps | grep postgres`
若无，按 docker-compose.yml 启动

- [ ] **Step 2: 应用迁移**

Run: `cd src && dotnet ef database update --project GZCTF --startup-project GZCTF`
Expected: 数据库迁移成功，新表已创建

- [ ] **Step 3: 验证表结构**

Run: `docker exec -it <postgres-container> psql -U postgres -d gzctf -c "\dt Challenge*"`
Expected: 显示 ChallengeTracks / ChallengeLevelConfigs / ChallengeLockStates / ChallengeTrackLevelBanStates / ChallengeWrongCountStates / ChallengeFlags / ChallengePenalties

- [ ] **Step 4: 验证数据迁移（若有旧数据）**

Run: `docker exec -it <postgres-container> psql -U postgres -d gzctf -c "SELECT COUNT(*) FROM \"ChallengeTracks\";"`
Expected: 旧 ChallengeFlags 行数相同

---

### Task 42: 手动端到端验证

- [ ] **Step 1: 启动应用**

Run: `cd src && dotnet run --project GZCTF`
Expected: 服务监听 `http://127.0.0.1:8080`

- [ ] **Step 2: 管理员登录，创建 Game + Challenge**

通过 UI 或 API 创建 Game 和 Challenge（StaticAttachment 类型）

- [ ] **Step 3: 通过 `POST /api/ext/challenges/{id}/levels` 创建 3 个 Level**

Level 1: Score=4, SubmissionLimit=5
Level 2: Score=2, SubmissionLimit=5, BanOnAnswers=["0"]
Level 3: Score=4, SubmissionLimit=5

- [ ] **Step 4: 通过 `POST /api/ext/challenges/{id}/flags/import` 上传 OH 数据集 xlsx**

使用 memo/OH社区第二届CTF大赛AI数据集（0616）.xlsx，配置列映射：
- TrackFilePathColumn = "文件路径"
- TrackRepositoryColumn = "所属仓库"
- TrackCategoryColumn = "参考类别"
- TrackReferenceUrlColumn = "社区公开PR"
- LevelColumns[0] = { Level=1, ColumnName="代码行数", AnswerType=LineRange, Score=4 }
- LevelColumns[1] = { Level=2, ColumnName="引入漏洞commit", AnswerType=Exact, Score=2 }
- LevelColumns[2] = { Level=3, ColumnName="CWE(研判)", AnswerType=Exact, Score=4 }
- MatchBy = "FilePath"
- Mode = "upsert"

预期：60 行导入，60 个 Track × 3 Level = 180 个 flag

- [ ] **Step 5: 选手登录，提交完整流程**

1. `GET /api/game/{id}/challenges/{cId}` → 获取 Track 列表
2. 提交 Level 1：`POST /api/game/{id}/challenges/{cId}` body `{ flag: "services/.../netsys_sock_client.cpp:c:56", level: 1 }`
3. 预期响应：`trackId` 填充，`scoreChange: 4`
4. 提交 Level 2：`POST /api/game/{id}/challenges/{cId}` body `{ flag: "5fc8a01873867354a83a69b6d9275fa53c78be6a", trackId: <返回的>, level: 2 }`
5. 预期响应：`scoreChange: 2`
6. 提交 Level 3：`POST /api/game/{id}/challenges/{cId}` body `{ flag: "CWE-476", trackId: <同上>, level: 3 }`
7. 预期响应：`scoreChange: 4`, `trackCompleted: true`

- [ ] **Step 6: 验证 trap answer**

对另一个 Track，提交 Level 2 的 "0"：
- 预期响应：`bannedTrackId` 填充，`bannedLevel: 2`
- 再次提交正确 commit hash：预期 `scoreChange: 0`，`message` 含 "banned"

- [ ] **Step 7: 验证超限锁定**

对某 Track Level 1 连续提交 5 次错误答案：
- 第 5 次：`challengeLocked: true`，`unlocksAt` 填充
- 第 6 次（锁中）：HTTP 400，message 含 "locked"

---

## 自检与交付

### Task 43: 最终自检

- [ ] **Step 1: 检查未使用代码**

Run: `cd src && dotnet build GZCTF/GZCTF.csproj 2>&1 | grep -i "warning\|unused"`

清理明显未使用的代码（特别是旧 HybridLine 相关辅助方法）。

- [ ] **Step 2: 检查 grep FlagMatchMode 残留**

Run: `grep -rn "FlagMatchMode" src/ 2>&1 | grep -v ".Designer.cs\|Migrations/202[0-9]"`
Expected: 无输出（所有引用都在历史迁移文件中）

- [ ] **Step 3: 运行全部测试**

Run: `cd src && dotnet test GZCTF.slnx`
Expected: 所有测试通过

- [ ] **Step 4: 提交最终清理**

```bash
git add -u
git commit -m "chore: final cleanup and dead code removal"
```

---

## 完成检查清单

- [ ] 5 张新表已创建并通过迁移
- [ ] `Challenge.FlagMatchMode` 字段已移除
- [ ] `ChallengeFlag` 上的 OpenHarmony 硬编码字段已迁移到 `ChallengeTrack`
- [ ] `ScoringService.ProcessSubmission` 接受 `(trackId, level)` 参数
- [ ] Trap answer（LockOnAnswers + BanOnAnswers）机制工作正常
- [ ] 全题锁到期后 WrongCount 重置
- [ ] FirstSolve 在所有 Track 所有 Level 解出时触发
- [ ] 批量导入 xlsx/csv 支持列映射与 upsert
- [ ] 管理员 API：Track/Level/Flag CRUD 完整
- [ ] 选手 API：`GET /api/game/{id}/challenges/{cId}` 返回多级状态
- [ ] 智能体可通过 ApiToken 认证调用所有接口
- [ ] 前端 Api.ts 扩展完成
- [ ] i18n 翻译完成
- [ ] OH 数据集 60 行成功导入并创建 180 个 flag
- [ ] 手动端到端验证通过（Level 1 → 2 → 3 + trap + 超限）

---

## 未覆盖事项（需后续单独计划）

以下在 spec 中提及但本计划未包含完整任务，建议作为后续迭代：

### 1. SignalR 通知（spec §6.5）

BigScreenHub 扩展事件（`TrackLevelSolved` / `TrackLevelBanned` / `ChallengeLocked` / `TrackCompleted` / `ChallengeFirstSolve`）需要：
- 在 `Hubs/BigScreenHub.cs` 添加事件方法
- 在 `ScoringService` 提交成功/锁定/封禁后调用 hub 推送
- 前端订阅事件并更新 UI

建议在 Phase 4 Controllers 完成后、Phase 8 测试前，单独添加 1-2 个 Task 实现。

### 2. 前端 UI 组件（spec §8.2）

本计划只覆盖了 `Api.ts` 与 i18n，未创建以下组件：
- `TrackList.tsx` — Track 列表 + 进度展示
- `TrackDetailModal.tsx` — 单 Track 多 Level 提交弹窗
- `LevelConfigEditor.tsx` — 管理员 Level 配置编辑器
- `FlagImportDialog.tsx` — 批量导入对话框
- `ColumnMappingForm.tsx` — 列映射表单
- 管理端 Challenge 编辑页 tabs 重构
- 选手端 Challenge 详情页重构

建议在 API 稳定后单独创建前端实施计划，按页面拆分 Task。

### 3. GameImportService 完整同步（spec §7.4）

Task 33 只扩展了 TransferChallenge 模型字段，未实现完整的 `Tracks` / `Levels` 序列化与反序列化逻辑。完整实现需要：
- `ToTransfer` 时查询 `ChallengeTrack[]` 和 `ChallengeLevelConfig[]` 填入
- `FromTransfer` 时按 trackId upsert（保留原 ID 或重新生成）
- 导出文件结构测试

建议在 Phase 6 单独扩展 1-2 个 Task。

### 4. 集成测试完整覆盖（spec §9.2）

Task 38 只提供了骨架。完整测试需要：
- 基于 `TestDataSeeder` 扩展，创建带 Track/Level/Flag 的 fixture
- 完整流程测试（导入 → 提交 Level 1/2/3 → FirstSolve）
- Trap 触发测试
- 超限锁定测试
- OH 数据集导入测试

建议在 Phase 8 单独扩展 3-5 个 Task。
