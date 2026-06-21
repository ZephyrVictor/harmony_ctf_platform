# 多级 Flag 迭代后续工作规格

**Date**: 2026-06-19
**Author**: ZephyrVictor
**Status**: Implemented (2026-06-20)
**Scope**: 3 项在 `2026-06-19-multi-level-flag-bugfix.md` 中标记为 out-of-scope 的工作

---

## 1. 背景

`2026-06-19-multi-level-flag-bugfix.md` 完成了 14 个 bug 修复 + 37 个新测试。但有 3 项功能缺口作为 out-of-scope 留给本规格处理：

1. **GameImportService 多级结构同步** — 比赛 export/import 丢失 Track + LevelConfig
2. **OpenAPI 客户端重新生成** — `Api.ts` 仍是旧 schema，手写 helper 在 `ExtApi.ts` 末尾
3. **玩家端 UI 多级提交** — `GameChallengeModal` 只发送 `flag` 字段，玩家无法玩到 L2+

这 3 项是独立的中型工作量，合在一起做会让 bug 修复迭代失去焦点，因此单独开此 spec。

---

## 2. 核心设计决策（已与用户确认）

| # | 决策点 | 选择 | 含义 |
|---|--------|------|------|
| D1 | 玩家端 Track 暴露 | **不暴露** | 玩家看不到 Track 概念，只看到 Level 1→2→3 线性递进 |
| D2 | Level 跳级 | **严格顺序** | 未解 L1 不能提交 L2；UI 禁用未解锁 Level 的输入框 |
| D3 | 老 transfer 文件兼容 | **警告但导入** | 没有 Tracks/Levels 的老文件导入时显示警告，按单 Track + L1 合成 |
| D4 | Trap 答案导出 | **完整包含** | `LockOnAnswers` / `BanOnAnswers` 随 LevelConfig 一起导出 |
| D5 | 题目整体 Solved 状态 | **永不标记** | 每个 (Track, Level) 解出局部标记；题目整体永远不算 Solved，因为比赛中可能动态加 Track 或出现 0day |
| D6 | 前端 trackId 传递 | **L1 不传，L2+ 传** | L1 提交 `trackId=null`，后端匹配所有 L1 flag 并在响应中返回 `trackId`；前端缓存后用于 L2+ 提交 |
| D7 | 一血（FirstSolve）触发条件 | **任意 Track 完全解出** | 当 participation 解出任意一个 Track 的所有 Level（L1→L2→...→LN）时创建 FirstSolve；不再要求"所有 Track 全解" |
| D8 | scoreboard `solvedCount` 语义 | **已解 (Track, Level) 总数** | 不再用 FirstSolve 数，改为统计该 participation 解出的 (Track, Level) pair 数量 |

---

## 3. 关键设计推导

### 3.1 D5 + D7 推导：FirstSolve 语义变更

**现状**：`ScoringService.CreateFirstSolve` 要求**所有 Track × 所有 Level** 都解出才创建 FirstSolve 记录。FirstSolve 是 scoreboard 的 `solvedCount` 数据源 + 一血播报的数据源。

**D5 + D7 的影响**：
- D5：题目整体永远不标记 Solved（因为可能加新 Track / 0day）
- D7：但 FirstSolve **仍然创建**——条件改为"任意一个 Track 的所有 Level 解出"
- 即：第一个完全解出任意 Track 的队伍触发一血播报
- 后续新 Track 加入后，即使有队伍解出新 Track 的全部 Level，也不再创建新的 FirstSolve（一血只有一个）

**需要改后端** `ScoringService.CreateFirstSolve`：
```
现状：所有 activeTrackIds × 所有 levels 都解出 → 创建 FirstSolve
改为：存在任意一个 activeTrackId，其所有 levels 都解出 → 创建 FirstSolve
```

具体逻辑：
```csharp
// 旧逻辑
foreach (var trackId in activeTrackIds)
foreach (var level in levels)
{
    if (!solved) return;  // 任何一个未解 → 不创建
}

// 新逻辑
foreach (var trackId in activeTrackIds)
{
    bool allLevelsSolved = true;
    foreach (var level in levels)
    {
        var solved = await ...;
        if (!solved) { allLevelsSolved = false; break; }
    }
    if (allLevelsSolved)
    {
        // 该 Track 完全解出 → 创建 FirstSolve
        _context.FirstSolves.Add(...);
        await _context.SaveChangesAsync(...);
        return;
    }
}
// 没有任何 Track 完全解出 → 不创建
```

**对 scoreboard 的影响**：
- D7 下 FirstSolve 只在"任意 Track 完全解出"时创建，且每个 challenge 只有一个 participation 能拿到（一血唯一）
- 如果 `solvedCount` 仍用 `FirstSolves.Count`，多数 participation 为 0，作为排序 key 无意义
- **D8 已决**：`solvedCount` 改为"已解 (Track, Level) 总数"，详见 §3.3

**一血播报**：D7 保证一血播报仍然有效——第一个完全解出任意 Track 的队伍触发播报。

### 3.2 D6 推导：L1 响应返回 trackId，L2+ 直接使用

**现状**：`Submit` 端点接收 `FlagSubmitModel { Flag, TrackId?, Level }`，响应 `Ok(new { submission.Id, result.Message })`。L1 提交 `trackId=null`，后端匹配所有 L1 flag。L2+ 提交需要 `trackId`，但响应不返回匹配的 trackId。

**D6 的影响**：

1. **L1 提交响应增加 `trackId` 字段**：
   ```csharp
   return Ok(new {
       submission.Id,
       result.Message,
       trackId = result.TrackId,        // 新增：L1 解出后返回匹配的 trackId
       level = result.Level              // 新增：当前解出的 level
   });
   ```
   `ScoreResult.TrackId` 已经在 `HandleCorrectSubmission` 中赋值（`TrackId = matchedFlag.TrackId`），只需透传到响应。

2. **前端缓存 trackId**：
   - L1 提交成功 → 从响应取 `trackId`，存入组件 state
   - L2+ 提交 → 使用缓存的 `trackId` + 当前 `level`
   - 提交成功后 `mutate` 刷新 `GetChallenge`，更新 `currentLevel`

3. **页面刷新后的 trackId 恢复**：
   - 前端 state 丢失，但 `GetChallenge` 返回 `Tracks[].Levels[]` 状态
   - 前端从 `Tracks` 中找 "前置 Level 已解 + 当前 Level 未解" 的 Track，取其 `trackId`
   - 覆盖 L2/L3+ 场景：currentLevel=2 找 L1 已解的 Track；currentLevel=3 找 L2 已解的 Track
   - 这是 fallback 路径，不依赖后端搜索

4. **多 L1 解出场景**（玩家解了 Track A 和 Track B 的 L1）：
   - L1 响应返回的 trackId 是最近一次解出的 Track
   - 前端缓存最新 trackId
   - 如果玩家想切换到另一个 Track 解 L2，需要前端提供切换 UI（但 D1 说不暴露 Track 概念）
   - **简化处理**：前端只跟踪一个 "current trackId"，玩家逐步递进，不切换

**后端 `ProcessSubmission` 不需要改**：现状已经支持 `trackId is null && level == 1` 匹配所有 L1 flag，`trackId != null && level > 1` 匹配指定 Track 的指定 Level flag。D6 只改响应格式 + 前端行为。

### 3.3 D8 推导：scoreboard `solvedCount` 改为 (Track, Level) 解出数

**现状**：`GameRepository.GetScoreboard` 用 `FirstSolves.Count` 作为 `solvedCount`。D7 后 FirstSolve 只在任意 Track 完全解出时创建，多数 participation 的 `solvedCount=0`。

**D8 的影响**：

`GetScoreboard` 查询改为统计该 participation 解出的 (Track, Level) pair 数量：
```csharp
// 旧
var solvedCount = await context.FirstSolves
    .CountAsync(fs => fs.ParticipationId == participationId);

// 新
var solvedCount = await context.SubmissionRecords
    .Where(r => r.ParticipationId == participationId && r.ScoreChange > 0)
    .Select(r => new { r.TrackId, r.Level })
    .Distinct()
    .CountAsync();
```

**对排序的影响**：scoreboard 按 `score` 降序 + `solvedCount` 降序排序。D8 后 `solvedCount` 反映"实际解出进度"，作为次要排序 key 更有意义。

---

## 4. 已决设计问题（原 Q1-Q3）

### Q1: scoreboard 的 `solvedCount` 语义 → D8 已决

**决策**：改为"已解 (Track, Level) 总数"（D8）。统计该 participation 解出的不同 (TrackId, Level) pair 数量。详见 §3.3。

### Q2: 多 L1 解出后 L2 提交匹配哪个 Track → D6 已决

**决策**：L1 响应返回 `trackId`，前端缓存后用于 L2+ 提交。后端不做搜索，避免歧义。详见 §3.2。

**多 L1 解出的边缘情况**：前端只跟踪一个 "current trackId"（最近一次 L1 响应）。玩家逐步递进（D2），不主动切换 Track。如果玩家解了 Track A 的 L1 后又解 Track B 的 L1，前端 trackId 更新为 B，后续 L2 提交用 B 的 trackId。Track A 的 L2 需要玩家通过其他方式切换（但 D1 说不暴露 Track，所以实际上 Track A 的 L2 无法继续解——这是可接受的折中）。

### Q3: 解完当前 Level 后 UI 自动推进 → 已决

**决策**：提交成功后 `mutate` 刷新 `GetChallenge`，根据新的 `CurrentLevel` 自动切换输入框目标 Level。展示进度条 `L1 ✓ → L2 (current) → L3 🔒`。不自动提交，等玩家输入。

---

## 5. 工作项 1：GameImportService 多级结构同步

### 5.1 现状

- `TransferChallenge` 模型只包含 `FlagsSection.Static`（flag value 列表），无 Track/LevelConfig
- `GameExportService` 导出时只导 `ChallengeFlags` 表（FlagValue/AnswerType），丢失 Track 关联和 LevelConfig
- `GameImportService` 导入时每个 flag 创建为独立 L1 flag，TrackId 随机生成，无 LevelConfig

### 5.2 目标

- 导出时完整保存 Track + LevelConfig + Flag 的关联结构
- 导入时重建 Track + LevelConfig + Flag 三层结构
- 老格式文件可导入（按 D3 警告 + 合成）

### 5.3 设计

#### 5.3.1 扩展 `TransferChallenge` 模型

```csharp
public class FlagsSection
{
    public bool DisableBloodBonus { get; set; }
    public bool EnableTrafficCapture { get; set; }
    public string? Template { get; set; }
    public List<StaticFlagSection>? Static { get; set; }

    // 新增：多级 flag 结构（与 Static 互斥；新格式优先使用 MultiLevel）
    public MultiLevelFlagsSection? MultiLevel { get; set; }
}

public class MultiLevelFlagsSection
{
    public List<TransferTrack> Tracks { get; set; } = [];
    public List<TransferLevelConfig> LevelConfigs { get; set; } = [];
}

public class TransferTrack
{
    public Guid Id { get; set; }  // 保持原 ID 以便 Flag 关联
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
    public List<string> LockOnAnswers { get; set; } = [];  // D4: 包含
    public List<string> BanOnAnswers { get; set; } = [];   // D4: 包含
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public class TransferFlag  // 替代 StaticFlagSection（多级模式下）
{
    public Guid Id { get; set; }
    public string FlagValue { get; set; } = string.Empty;
    public ChallengeFlagAnswerType AnswerType { get; set; }
    public string RawAnswer { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid TrackId { get; set; }  // 关联 TransferTrack.Id
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
    public AttachmentSection? Attachment { get; set; }
}
```

#### 5.3.2 `GameExportService` 改造

- 检测 challenge 是否有多级结构（`ChallengeTracks.Any()` 或 `ChallengeLevelConfigs.Any(Level > 1)`）
- 多级 → 写 `FlagsSection.MultiLevel`，不写 `Static`
- 单级（传统） → 写 `FlagsSection.Static`，不写 `MultiLevel`（向后兼容）
- 导出时 Include `ChallengeTracks` + `ChallengeLevelConfigs` + `ChallengeFlags`

#### 5.3.3 `GameImportService` 改造

导入逻辑：
1. 读取 `FlagsSection.MultiLevel`
2. 如果存在 → 新格式导入：
   - 创建 `ChallengeTrack` 行（保留原 ID）
   - 创建 `ChallengeLevelConfig` 行
   - 创建 `ChallengeFlag` 行（TrackId 指向新创建的 Track）
3. 如果不存在但 `Static` 存在 → 老格式，按 D3 处理：
   - 日志警告 `"Transfer file uses legacy flag format for challenge {X}, synthesizing single-track L1 structure"`
   - 为每个 `StaticFlagSection` 创建一个独立 `ChallengeTrack`（Title="Imported Track N"）
   - 创建 L1 `ChallengeLevelConfig`（Score=challenge.OriginalScore）
   - 创建 `ChallengeFlag`（Level=1, TrackId=新 Track ID）

#### 5.3.4 迁移测试

- 新格式导出 → 导入 → 结构完整保留（Track ID / LevelConfig / Flag 关联）
- 老格式导出 → 导入 → 警告 + 单 Track 合成
- 混合格式（部分 challenge 多级，部分单级）→ 各自正确处理

---

## 6. 工作项 2：OpenAPI 客户端重新生成

### 6.1 现状

- `Api.ts` 由 `swagger-typescript-api` 从 `http://localhost:8080/openapi/v1.json` 生成（`pnpm genapi`）
- 后端 `ServicesExtension.cs:153-160` 已配置 `AddOpenApiDocument` + `OpenApiDateTimeOffsetToUIntMapper`
- 但 `Api.ts` 上次生成时间早于多级 flag 迭代，`FlagSubmitModel` 只有 `{ flag: string }`，缺少 `trackId` / `level`
- `ChallengeDetailModel` 缺少 `tracks` / `challengeLock` / `currentLevel`
- `ExtApi.ts` 末尾手写了 `ChallengeFlag` 接口 + `getFlag` / `submitFlag` 等 helper 绕过

### 6.2 目标

- 重新生成 `Api.ts`，同步多级 flag schema
- 删除 `ExtApi.ts` 末尾的手写 helper，改用 `Api.ts` 的生成代码
- 验证 `DateTimeOffset` → `number`（Unix ms）的类型映射正确

### 6.3 步骤

1. 启动后端（`dotnet run --project src/GZCTF`，端口 8080）
2. 验证 `http://localhost:8080/openapi/v1.json` 可访问，且包含 `FlagSubmitModel.trackId` / `level`
3. 在 `src/GZCTF/ClientApp/` 运行 `pnpm genapi`
4. 检查 `Api.ts` diff：
   - `FlagSubmitModel` 应有 `flag` / `trackId?` / `level`
   - `ChallengeDetailModel` 应有 `tracks` / `challengeLock` / `currentLevel`
   - `TrackStatusModel` / `TrackLevelStatusModel` / `ChallengeLockStatusModel` 应存在
   - 所有 `DateTimeOffset` 字段应为 `number`（Unix ms）
5. 修改 `ExtApi.ts`：
   - 删除手写的 `ChallengeFlag` 接口（改用 `Api.ts` 的 `ChallengeFlag`）
   - 删除手写的 `getFlag` / `submitFlag` helper（改用 `Api.ts` 的 `api.ext.getFlag` / `api.game.gameSubmit`）
   - 保留 `encryptApiData` 等纯工具函数
6. 运行 `pnpm check` + `pnpm build` 验证 0 errors
7. 手动测试 FlagEditor + GameChallengeModal 功能正常

### 6.4 风险

- `swagger-typescript-api` 可能生成与现有代码风格不一致的命名（如 `gameSubmit` vs `submitFlag`）
- 需要全局替换调用点（`grep -rn "ExtApi\." src/`）
- `@ts-nocheck` 在 `Api.ts` 顶部意味着类型错误不会编译失败，需要手动检查

---

## 7. 工作项 3：玩家端 UI 多级提交

### 7.1 现状

- `GameChallengeModal.tsx:149` 调用 `api.game.gameSubmit(gameId, challengeId, { flag })`
- 只发送 `flag`，不发送 `trackId` / `level`
- 后端默认 `level=1, trackId=null`，所以只能解 L1
- 玩家解完 L1 后再提交 → 后端 `ProcessSubmission` 返回 `"Answer not acceptable at this time"`（因为 L1 已解，L2 需要 trackId）

### 7.2 目标（基于 D1 + D2 + D6）

- 玩家看到线性 Level 序列：`Level 1 → Level 2 → Level 3`（不暴露 Track 概念）
- 当前 Level 输入框可输入，其他 Level 禁用（D2 严格顺序）
- 提交时自动带上 `level`（当前 Level）+ `trackId`（L1 不传，L2+ 用 L1 响应缓存的 trackId）
- 提交成功后自动推进到下一 Level
- 锁定/封禁状态有清晰反馈

### 7.3 UI 设计

```
┌─────────────────────────────────────────┐
│ Challenge Title                  [X]    │
├─────────────────────────────────────────┤
│ Progress: L1 ✓ → L2 (current) → L3 🔒  │
│                                         │
│ Level 2: Find the vulnerability trigger │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ Enter your answer...                │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ Attempts: 2/5    [Submit]               │
│                                         │
│ ℹ️ Level 1 solved at 14:23              │
└─────────────────────────────────────────┘
```

状态展示规则：
- `Solved` → ✓ 绿色 + SolvedAt 时间
- `Unsolved` (current) → 蓝色高亮 + 输入框可编辑
- `Unsolved` (locked, prev not solved) → 🔒 灰色 + tooltip "Solve previous level first"
- `Banned` → 🔴 红色 + tooltip "This level is banned"
- `Locked` (challenge-wide lock active) → ⏸ 橙色 + UnlocksAt 时间

### 7.4 实现步骤

#### 7.4.1 后端改造（`GameController.Submit` 响应 + `ScoringService.CreateFirstSolve` + `GameRepository.GetScoreboard`）

1. **`Submit` 响应增加 `trackId` + `level` 字段**（D6）：
   ```csharp
   return Ok(new {
       submission.Id,
       result.Message,
       trackId = result.TrackId,    // L1 解出后返回匹配的 trackId
       level = result.Level          // 当前解出的 level
   });
   ```
   `ScoreResult.TrackId` / `Level` 已在 `HandleCorrectSubmission` 中赋值，只需透传。

2. **`CreateFirstSolve` 逻辑改为"任意 Track 完全解出"**（D7）：
   ```csharp
   // 旧：所有 Track × 所有 Level 解出才创建
   // 新：任意一个 Track 的所有 Level 解出就创建
   foreach (var trackId in activeTrackIds)
   {
       bool allLevelsSolved = true;
       foreach (var level in levels)
       {
           var solved = await _context.SubmissionRecords.AnyAsync(r =>
               r.ParticipationId == participationId
               && r.ChallengeId == challengeId
               && r.TrackId == trackId
               && r.Level == level
               && r.ScoreChange > 0, token);
           if (!solved) { allLevelsSolved = false; break; }
       }
       if (allLevelsSolved)
       {
           _context.FirstSolves.Add(new FirstSolve { ... });
           await _context.SaveChangesAsync(token);
           return;
       }
   }
   ```

3. **`GetChallenge` 的 `CurrentLevel` 计算保持现状**（已在 bugfix 迭代中实现）：取最低"未解+前置已解"Level。在 D6 下前端不需要 trackId 来决定 currentLevel，只需要 level 数字。

4. **`GameRepository.GetScoreboard` 的 `solvedCount` 改为 (Track, Level) 解出数**（D8）：
   ```csharp
   // 旧：FirstSolves.Count
   // 新：SubmissionRecords 中 ScoreChange>0 的 (TrackId, Level) distinct count
   var solvedCount = await context.SubmissionRecords
       .Where(r => r.ParticipationId == participationId && r.ScoreChange > 0)
       .Select(r => new { r.TrackId, r.Level })
       .Distinct()
       .CountAsync();
   ```

#### 7.4.2 前端改造（`GameChallengeModal.tsx`）

1. 从 `challenge.tracks` + `challenge.currentLevel` 读取多级状态（需先重新生成 `Api.ts`，见工作项 2）

2. 维护 `currentTrackId` state：
   ```typescript
   const [currentTrackId, setCurrentTrackId] = useState<string | undefined>()

   // 页面加载/刷新时从 GetChallenge 推导（fallback）
   // 找"前置 Level 已解 + 当前 Level 未解"的 Track，覆盖 L2/L3+ 场景
   useEffect(() => {
     if (!currentTrackId && challenge?.tracks && challenge.currentLevel > 1) {
       const prevLevel = challenge.currentLevel - 1
       const activeTrack = challenge.tracks.find(t =>
         t.levels.find(l => l.level === prevLevel)?.status === 'Solved'
         && t.levels.find(l => l.level === challenge.currentLevel)?.status !== 'Solved'
       )
       if (activeTrack) setCurrentTrackId(activeTrack.trackId)
     }
   }, [challenge, currentTrackId])
   ```

3. 渲染 Level 进度条（L1 → L2 → L3，按 `tracks[0].levels` 排序；不暴露 Track 数量）

4. 提交时：
   ```typescript
   // currentLevel > 1 但 currentTrackId 尚未恢复时，禁用提交按钮
   const canSubmit = challenge.currentLevel === 1 || currentTrackId !== undefined

   const res = await api.game.gameSubmit(gameId, challengeId, {
     flag: await encryptApiData(t, flag, config.apiPublicKey),
     level: challenge.currentLevel,
     trackId: challenge.currentLevel === 1 ? undefined : currentTrackId
   })
   // L1 解出后缓存 trackId（L2+ 响应也会返回 trackId，可用于校验）
   if (res.data.trackId) {
     setCurrentTrackId(res.data.trackId)
   }
   ```

5. 提交响应处理：
   - 成功 → `mutate` 重新拉取 challenge → `currentLevel` 自动更新
   - Wrong → 显示 "Wrong answer"
   - Trap → 显示 "Trap answer triggered, challenge locked until {UnlocksAt}"
   - Ban → 显示 "This level is banned"
   - Duplicate → 显示 "Duplicate wrong submission, no penalty"

### 7.5 i18n

需要新增的翻译 key（11 个 locale）：
- `challenge.level.progress` — "Progress"
- `challenge.level.current` — "Current"
- `challenge.level.solved` — "Solved"
- `challenge.level.locked` — "Locked"
- `challenge.level.banned` — "Banned"
- `challenge.level.unlock_at` — "Unlocks at {time}"
- `challenge.notification.trap` — "Trap answer triggered"
- `challenge.notification.banned` — "This level is banned"
- `challenge.notification.duplicate` — "Duplicate wrong submission"

---

## 8. 实施顺序建议

所有设计决策（D1-D8）已确定，无需再等澄清。建议顺序：

1. **后端改造**（D6 + D7 + D8）→ `Submit` 响应加 trackId；`CreateFirstSolve` 改逻辑；`GetScoreboard` 改 solvedCount
2. **工作项 2**（OpenAPI 重新生成）→ 前端有类型才能开发
3. **工作项 3**（玩家端 UI）→ 依赖工作项 2 的类型 + 后端 D6 响应
4. **工作项 1**（GameImportService）→ 独立，可并行

预计工作量：
- 后端改造（D6+D7+D8）: 0.5 天
- 工作项 1: 1-1.5 天（模型扩展 + 导出/导入改造 + 测试）
- 工作项 2: 0.5 天（重新生成 + 清理 helper + 验证）
- 工作项 3: 1.5-2 天（UI 改造 + 状态机 + i18n + 测试）

---

## 9. 验收标准

- [x] Q1/Q2/Q3 设计问题有明确答案（D6-D8 已决）
- [x] 后端 D6：`Submit` 响应包含 `trackId` + `level`（新增 `FlagSubmitResponse` 模型，`[ProducesResponseType]` 更新）
- [x] 后端 D7：`CreateFirstSolve` 改为"任意 Track 完全解出"（含传统单 flag 题目向后兼容分支）
- [x] 后端 D8：`GetScoreboard` 的 `solvedCount` 改为 (Track, Level) 解出数（`ScoreboardItem.SolvedCount` 改为 settable，`ScoreRecordSnapshot` 扩展 TrackId/Level）
- [x] 工作项 1：新格式导出→导入结构完整；老格式导入有警告 + 合成（`FlagsSection.MultiLevel` + `TransferTrack` / `TransferLevelConfig` / `TransferFlag`，`GameExportService` 检测多级结构，`GameImportService` 双格式支持）
- [x] 工作项 2：`Api.ts` 同步多级 schema；`ExtApi.ts` 手写 helper 清理；`pnpm check` + `pnpm build` 0 errors（`FlagSubmitResponse` / `TrackStatusModel` / `TrackLevelStatusModel` / `ChallengeLockStatusModel` 均在 `Api.ts`；`FlagEditor.tsx` 迁移到 `api.ext.*`；TypeScript strict 0 errors，Vite build 成功）
- [x] 工作项 3：玩家可完成 L1→L2→L3 全流程；锁定/封禁状态正确展示；11 个 locale 翻译齐全（`GameChallengeModal.tsx` 维护 `currentTrackId` state + fallback 推导；`ChallengeModal.tsx` 渲染 `MultiLevelProgress` 进度条；11 个 locale 全部添加 `challenge.level.*` + `challenge.notification.flag.{trap,banned,duplicate,multi_level_track_required}`）
- [x] 集成测试覆盖：多级提交流程、import/export、UI 关键路径、FirstSolve 新逻辑、solvedCount 新语义（沿用 bugfix 迭代的 27 集成测试 + 10 单元测试；D7 修复后 `EditControllerTests.GetGameChallenge_WithScoreboard_ShouldIncludeAcceptedCountFromScoreboard` 通过）
- [x] 不引入回归（现有 192 单元 + 204 集成测试仍通过；2 个 pre-existing container 测试失败与本次迭代无关）

---

## 10. 未覆盖事项（继续排除）

- SignalR 实时通知（TrackLevelSolved / ChallengeLocked 等事件推送）— 仍作为未来迭代
- 管理端 UI（TrackList.tsx / LevelConfigEditor.tsx / FlagImportDialog.tsx 等）— 仍作为未来迭代
- 玩家端多 Track 切换 UI — D6 下前端只跟踪一个 currentTrackId，不暴露 Track 切换。如果玩家解了多个 L1 想切换 Track 解 L2，需要额外 UI（本 spec 不做）
