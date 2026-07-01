# 题目级限流（答错次数 + 冷却）实现计划 v2

> 日期：2026-07-01
> 范围：替换现有 SubmissionLimit + 全局 RateLimiter middleware，改成 per-challenge 答错计数 + 临时冷却
> 与 trap/ChallengeLockState 完全独立，trap 机制不受影响

## 需求对齐结果（v2）

- **粒度**: per-challenge（整题，所有 track/level 合计错答次数）
- **冷却行为**: 临时冷却，自动解封
- **错误次数阈值**: **不可配**，自动 = `trackCount × 5`（如 ecmascript/base 有 12 track → 60 次）
- **冷却时间**: **可配**，per-game（比赛编辑页），默认 5 分钟
- **与 SubmissionLimit**: 替换（删除字段 + 相关代码）
- **与全局 RateLimiter middleware**: 替换（删除整个 middleware + 注册 + attribute）
- **与 trap (ChallengeLockState)**: 独立，互不影响
- **配置位置**: per-game（比赛编辑页 CooldownMinutes 一个字段）

---

## 架构设计

### 数据模型变更

#### 1. Game 新增 1 字段
```csharp
// src/GZCTF/Models/Data/Game.cs
/// <summary>
/// 题目冷却分钟数。答错达阈值后封锁该题 X 分钟。0 = 不限流
/// </summary>
public int CooldownMinutes { get; set; } = 5;
```

#### 2. 新建 ChallengeCooldownState 表
```csharp
// src/GZCTF/Models/Data/ChallengeCooldownState.cs
[Index(nameof(ParticipationId), nameof(ChallengeId), IsUnique = true)]
public class ChallengeCooldownState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }

    /// <summary>
    /// 当前周期错答次数（达阈值后重置为 0 并进入冷却）
    /// </summary>
    public int WrongCount { get; set; }

    /// <summary>
    /// 冷却到期时间。null = 未冷却；> now = 冷却中
    /// </summary>
    public DateTimeOffset? CooldownUntil { get; set; }

    public DateTimeOffset LastWrongAt { get; set; } = DateTimeOffset.UtcNow;
}
```

#### 3. 删除（死代码清理）
- `ChallengeLevelConfig.SubmissionLimit` 字段 + 配置代码
- `ChallengeWrongCountState` 模型 + 表（per-level 错答计数，被新表替代）
- `RateLimiter.cs` 整个文件（middleware 层限流）
- `ServicesExtension.cs` 里 `AddRateLimiter` 注册
- `AppExtensions.cs` 里 `UseRateLimiter` 调用
- 所有 `[EnableRateLimiting(...)]` attribute（GameController、AccountController 等）
- `LimitPolicy` enum
- `NoticeType.RateLimitAlert` 枚举值 + GameNoticeRepository 里 GetAlertNotices 方法 + EditController.AlertNotices endpoint
- `FlagImportModels.LevelColumnMapping.SubmissionLimit` 字段
- 前端 `AdminBanApi.ts` 里 `listAlertNotices` 相关代码
- `Bans.tsx` 里告警区 UI（因为不再有 RateLimitAlert）

### 提交流程变更（ScoringService.SubmitFlag）

```
1. 检查冷却:
   state = cooldownRepo.Get(participationId, challengeId)
   if state?.CooldownUntil is not null && state.CooldownUntil > now:
     return WrongAnswer($"题目冷却中，剩 {mm:ss}")

2. 验证 flag (现有逻辑，包括 trap 检查、ban 检查)

3. 如果答错:
   state.WrongCount++
   state.LastWrongAt = now
   trackCount = trackRepo.CountTracks(challengeId)
   limit = trackCount * 5
   if state.WrongCount >= limit:
     state.CooldownUntil = now + TimeSpan.FromMinutes(game.CooldownMinutes)
     state.WrongCount = 0  // 重置，下个周期重新计
   cooldownRepo.Upsert(state)

4. 如果答对:
   state.WrongCount = 0
   state.CooldownUntil = null
   cooldownRepo.Upsert(state)
```

### API 变更

#### 玩家题目详情 `GET /api/game/{id}/Challenges/{chalId}` 响应增加:
```json
{
  "cooldown": {
    "wrongCount": 3,
    "wrongAnswerLimit": 60,   // = trackCount * 5
    "cooldownMinutes": 5,
    "cooldownUntil": null     // 或 ISO 时间
  }
}
```

#### Admin 比赛编辑 `GET/PUT /api/edit/Games/{id}` 增加 `cooldownMinutes` 字段

### 前端变更

#### 1. Admin Info.tsx
在"0day 提交说明"卡片下方加"限流配置"卡片：
- NumberInput: 冷却分钟数 (CooldownMinutes, 默认 5, 0=不限)
- 说明文字："答错次数阈值为题目 track 数 × 5，自动计算"

#### 2. Admin Levels.tsx
移除 SubmissionLimit NumberInput

#### 3. Admin Import.tsx + import-flags.tsx
移除 levelCols 里 submissionLimit 字段

#### 4. Admin Bans.tsx
移除告警区（RateLimitAlert）UI，只保留手动封禁列表

#### 5. 选手 ChallengeModal.tsx
- 接收 cooldown prop
- 冷却中：禁用提交按钮 + 显示倒计时 "题目冷却中，剩 mm:ss"
- 否则：显示 "错答 X / limit"（如 3/60）

#### 6. i18n
zh-CN/en-US admin.json + game.json 加新 key

---

## 任务拆解

### Task 1: 后端数据模型 + 迁移
- 新建 `ChallengeCooldownState.cs`
- `Game.cs` 加 `CooldownMinutes`
- `AppDbContext` 注册 DbSet + entity config
- 生成迁移 `ChallengeCooldown`

### Task 2: 后端 Repository
- 新建 `IChallengeCooldownRepository` + 实现
  - `Get(participationId, challengeId)`
  - `Upsert(state)`
- 注册 DI

### Task 3: 后端 ScoringService 改造
- 注入 `IChallengeCooldownRepository`
- SubmitFlag 开头加冷却检查
- 答错时更新计数 + 触发冷却（limit = trackCount × 5）
- 答对时重置
- 返回冷却信息

### Task 4: 后端 Controller/DTO
- `GameController.Submit` 响应加 cooldown 字段
- `GameController.ChallengesWithTeamInfo` 题目详情加 cooldown
- `EditController` game DTO 加 CooldownMinutes
- `GameInfoModel` 加字段 + FromGame/Update 映射

### Task 5: 后端死代码清理
- 删除 `ChallengeLevelConfig.SubmissionLimit`
- 删除 `ChallengeWrongCountState` 模型 + DbSet
- 删除 `RateLimiter.cs` 整个文件
- 删除 `AddRateLimiter` / `UseRateLimiter` 注册
- 删除所有 `[EnableRateLimiting]` attribute
- 删除 `NoticeType.RateLimitAlert` + `GetAlertNotices` + `AlertNotices` endpoint
- 删除 `FlagImportModels.LevelColumnMapping.SubmissionLimit`
- 生成迁移删除 `ChallengeWrongCountStates` 表 + `ChallengeLevelConfigs.SubmissionLimit` 列

### Task 6: 前端类型 + API
- `Api.ts` 重新生成或手动加字段
- `ManualApiTypes.ts` 删 `LevelColumnMapping.submissionLimit`

### Task 7: 前端 Admin Info.tsx
- 加"限流配置"卡片（CooldownMinutes NumberInput + 说明）

### Task 8: 前端 Admin Levels.tsx
- 移除 SubmissionLimit 输入框

### Task 9: 前端 Admin Import.tsx + import-flags.tsx
- 移除 levelCols submissionLimit

### Task 10: 前端 Admin Bans.tsx
- 移除告警区 UI（RateLimitAlert 部分）

### Task 11: 前端 ChallengeModal.tsx
- 接收 cooldown prop
- 冷却中：禁用提交 + 倒计时
- 否则：显示 wrongCount/limit

### Task 12: 前端 i18n
- zh-CN/en-US admin.json: `admin.content.games.info.cooldown_minutes`
- zh-CN/en-US game.json: `game.content.cooldown.remaining` + `cooldown.count`

### Task 13: 重建镜像 + 后端测试
- `docker compose build gzctf && docker compose up -d gzctf`
- 应用迁移
- curl 测试：设 CooldownMinutes=1 → 故意答错 trackCount×5 次 → 验证冷却 → 等 1 分钟 → 重试

### Task 14: 前端 Chrome e2e
- 按 `docs/test-plans/2026-07-01-e2e-chrome.md` 执行

---

## 测试计划（后端 curl）

### 准备
```bash
# Admin 设 CooldownMinutes=1（快速测试）
curl -X PUT http://10.77.0.10:8081/api/edit/Games/1 -b /tmp/admin_cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{...,"cooldownMinutes":1}'
```

### 测试用例
1. **查询 trackCount**: challenge 1 有 12 track → limit = 60
2. **答错 59 次**: 不冷却，wrongCount=59
3. **答错第 60 次**: 触发冷却，wrongCount 重置为 0，CooldownUntil = now + 1min
4. **第 61 次提交**: 返回 "题目冷却中，剩 mm:ss"
5. **等 1 分钟后再提交**: 不再冷却，wrongCount=1
6. **答对 L1**: wrongCount 清零，CooldownUntil 清空
7. **per-challenge 隔离**: challenge 1 冷却中，challenge 2 不受影响
8. **per-team 隔离**: TeamA 冷却中，TeamB 不受影响
9. **CooldownMinutes=0**: 不限流，答错不冷却

---

## 风险与取舍

- **trackCount × 5 在 track 增减时变化**: 若导入新 track，limit 变大；删 track，limit 变小。但已记录的 wrongCount 不变。可接受
- **现有比赛迁移后 CooldownMinutes=5**: 合理默认
- **ChallengeWrongCountStates 表删除**: 表里数据丢失。但这表本来就没被使用（旧 SubmissionLimit 逻辑没用它），无影响
- **RateLimitAlert NoticeType 删除**: 之前从未触发过，无数据损失
- **trap (ChallengeLockState) 完全独立**: trap 是永久锁（直到管理员解封或 trap 答案匹配），跟冷却（临时自动解封）是两套机制，互不干扰
