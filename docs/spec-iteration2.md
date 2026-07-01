# OpenHarmony CTF 平台第二轮迭代开发文档

> 日期：2026-06-30
> 范围：申诉取消、0day 工单前端关闭、Flag 批量操作、队伍分数管理、封禁/告警、Level 名字显示、i18n 双语
> 测试计划文档在实现完成后单独编写，本文档不含测试计划

---

## 1. 概述

本轮迭代针对 GZCTF OpenHarmony CTF 适配平台做 6 项功能优化：

1. 取消申诉机制，0day 工单前端关闭（后端代码保留），新增"0day 提交说明"tab 跳转 bugbounty 平台
2. Flag 管理界面增加批量操作（多选 + 启用/禁用/删除）
3. 所有新增前端保持中英文双语（zh-CN + en-US）
4. 提交界面 Level 显示导入时的列名（而非裸 `L1/L2/L3`）
5. 管理端新增队伍分数 CRUD（加减分 + 理由 + 通报 + 搜索），0day 加分复用该接口
6. 限流不自动封禁改告警，管理员手动封/解封（Track, Level 粒度），trap 自动封保留

实现顺序按依赖排：F1→F6，每个 feature 完整走"后端实现 → 后端接口测试 → 前端实现 → Chrome e2e → 列 issue → 修 → 复测"。

---

## 2. 背景与现状

### 2.1 申诉机制
- 无独立子系统，申诉是 `TicketController` 在 penalty 提交上的工单流
- 前端在 `pages/games/[id]/Submissions.tsx` 点"发起申诉"
- i18n key 在 `locales/zh-CN/game.json`（"发起申诉"、"申诉描述"等）

### 2.2 0day 工单
- 完整存在：`ZeroDayTicketController`（Create/GetMine/Admin List/Approve/Reject）、`ZeroDayTicket` 模型、Repository、前端 player/admin 页面
- **入口在比赛前置 Index 页**（`pages/games/[id]/Index.tsx:227`），不在 WithGameTab 里
- 管理端在 `WithGameEditTab` 有独立 tab
- 级别→分值阶梯文档化在 `ZeroDayTicket.cs:34`：Critical N×100, High N×60, Medium N×30, Low N×10

### 2.3 Flag 管理
- `components/admin/FlagEditPanel.tsx`：卡片网格，每张卡只有复制 + 单删
- 无多选、无批量启停/删除

### 2.4 提交界面
- `components/ChallengeModal.tsx` 的 `MultiLevelProgress` 只显示 `L1/L2/L3`
- 后端 `TrackLevelStatusModel.Title` 字段已存在但前端未渲染
- `ChallengeLevelConfig.Title` 字段也存在但未使用

### 2.5 队伍分数管理
- **无手动调分接口**，无 `ScoreController`，无 `AdjustScore` endpoint
- `GameNotice` 模型有 CRUD（`EditController.cs:342-430`），类型 `FirstBlood/SecondBlood/ThirdBlood/Normal`
- 管理端 `pages/admin/games/[id]/Notices.tsx` 可编辑通知，但通知是展示消息，不是分数账本

### 2.6 封禁/限流
- `RateLimiter.cs`：`LimitPolicy.Submit` 是 TokenBucketLimiter（100 tokens, 50/period, 5s），全局 per-user
- Trap 答案自动封在 `(Track, Level)` 粒度（`ChallengeTrackLevelBanState`），永久
- **无管理员手动封/解封接口**，无可疑提交告警机制

### 2.7 i18n
- `react-i18next` + `locales/{locale}/*.json`，11 个 locale，zh-CN 权威，en-US 备用
- 新增 key 在 `zh-CN/*.json` 和 `en-US/*.json` 各加一份，其他 locale 回退 en-US

---

## 3. 需求清单

| # | 需求 | 验收标准 |
|---|---|---|
| R1 | 取消申诉前端入口 | Submissions 页面"发起申诉"按钮隐藏；后端 TicketController 代码保留不调用 |
| R2 | 0day 工单前端关闭 | 比赛前置 Index 页 0day 按钮移除；player 0day 工单页 + admin 0day 审批 tab 隐藏；后端代码保留 |
| R3 | 0day 提交说明 tab | WithGameTab 新增"0day 提交"tab，渲染 markdown 文案（Game.ZeroDayInfoMarkdown），文案含 bugbounty 链接 + 当前队伍 token/队名 |
| R4 | Flag 批量操作 | FlagEditPanel 卡片加 checkbox，选中后顶部浮现"启用/禁用/删除"批量操作栏 |
| R5 | Level 名字显示 | MultiLevelProgress 显示 `L{n} - {Title}`，Title 来自导入时列名；无 Title 回退 `L{n}` |
| R6 | 队伍分数 CRUD | 管理端新增"分数管理"tab（搜索队名/成员/token 模糊搜 + 调分弹窗）；"调分记录"账本子页；"0day 加分"快捷模板；"0day 加分记录"子页；调分写入 GameNotice 通报 |
| R7 | 封禁/解封 | 管理端新增"封禁管理"tab，支持 (Track, Level) 粒度手动封/解封；trap 自动封保留；可解封 trap 误触 |
| R8 | 限流告警 | 限流触发写入 GameNotice（RateLimitAlert 类型，admin-only 可见），不自动封禁 |
| R9 | i18n 双语 | 所有新增前端文案在 zh-CN 和 en-US 都有 key，无硬编码中文/英文 |

---

## 4. 数据模型变更

### 4.1 `Game` 模型新增字段

```csharp
// src/GZCTF/Models/Data/Game.cs
public string ZeroDayInfoMarkdown { get; set; } = string.Empty;
public int ZeroDayBaseScore { get; set; } = 1;
```

- `ZeroDayInfoMarkdown`：默认值通过迁移注入（内置中文模板，含 bugbounty 链接占位 + team token / 队名占位）。管理员在比赛编辑页可改。字段只存一份 markdown，不做 per-locale 切换；管理员可自行替换成英文或其他语言文案。默认模板见附录 A。
- `ZeroDayBaseScore`：0day 加分的基础倍数，默认 1。附录 B 的级别阶梯乘以该值得出实际加分（Critical = base×100, High = base×60, Medium = base×30, Low = base×10）。

### 4.2 新增 `ScoreAdjustment` 表

```csharp
// src/GZCTF/Models/Data/ScoreAdjustment.cs
public class ScoreAdjustment
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int Delta { get; set; }              // 正=加分, 负=扣分
    public string Reason { get; set; } = string.Empty;
    public ScoreAdjustmentCategory Category { get; set; }
    public string? RelatedInfo { get; set; }    // 0day: CVE 编号 + 级别等
    public Guid OperatorId { get; set; }
    public UserInfo Operator { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ScoreAdjustmentCategory { Manual, ZeroDayBonus }
```

- 0day 加分和手动调分都写这张表，`Category` 区分
- 0day 加分记录页 = `Category == ZeroDayBonus` 的过滤视图
- 队伍总分 = 基础分（submission 得分）+ Σ Delta（调分记录）
- 实时计算，不缓存到 Team 表（避免不一致）

### 4.3 `GameNotice` 扩展

```csharp
// src/GZCTF/Models/Data/GameNotice.cs
public enum NoticeType
{
    FirstBlood, SecondBlood, ThirdBlood, Normal,
    RateLimitAlert,      // 新增：限流告警，admin-only
    ScoreAdjustment,     // 新增：调分通报，全员可见
}
```

- `RateLimitAlert`：限流触发时写入，`Values` 含 `[teamName, challengeTitle, timestamp]`
- `ScoreAdjustment`：调分时写入，`Values` 含 `[teamName, delta, reason, operatorName]`
- 可见性控制：player-facing 查询排除 `RateLimitAlert`，admin 查询返回全部（见 5.4）

### 4.4 `ChallengeTrackLevelBanState` 扩展

```csharp
// src/GZCTF/Models/Data/ChallengeTrackLevelBanState.cs
// 现有字段：TeamId, TrackId, Level, BanAnswer, BannedAt
// 新增：
public string? Reason { get; set; }       // "Trap triggered" 或管理员填的理由
public Guid? BannedBy { get; set; }       // null=自动(trap), 非null=管理员
public bool IsManual { get; set; }        // true=手动封, false=trap 自动封
```

- Trap 自动封时：`IsManual=false`, `Reason="Trap triggered"`, `BannedBy=null`
- 管理员手动封时：`IsManual=true`, `Reason=管理员填写`, `BannedBy=当前管理员Id`
- 管理员解封：删除记录（trap 误触和手动封都可解封）

### 4.5 `ChallengeLevelConfig.Title` 启用

- 字段已存在（`src/GZCTF/Models/Data/ChallengeLevelConfig.cs`），目前未使用
- 导入时（`FlagImportService.cs`）将 `columnName` 写入 `Title`
- 前端 `MultiLevelProgress` 读取 `Title` 显示

### 4.6 不删除的代码

| 模块 | 处理 |
|---|---|
| `TicketController` / `Ticket` 模型 / 申诉 i18n key | 保留，仅前端入口隐藏 |
| `ZeroDayTicketController` / `ZeroDayTicket` / Repository | 保留，仅前端入口隐藏 |
| `pages/games/[id]/zeroday-tickets.tsx`（player） | 保留文件，从路由和导航移除 |
| `pages/admin/games/[id]/zeroday-tickets.tsx`（admin） | 保留文件，从 WithGameEditTab 移除 |

---

## 5. 后端 API 变更

### 5.1 新增 `ScoreAdjustmentController`

路由前缀：`api/admin/games/{gameId}/score-adjustments`

| Method | Path | 说明 |
|---|---|---|
| POST | `/` | 创建调分记录（手动或 0day 加分），body: `{ teamId, delta, reason, category, relatedInfo? }` |
| GET | `/?category=&teamId=&page=&pageSize=` | 调分记录列表（分页 + 过滤），管理员用 |
| GET | `/zero-day-bonus` | 0day 加分记录（`Category==ZeroDayBonus` 的快捷过滤） |
| GET | `/teams?q=` | 搜索队伍（队名/成员/Token 模糊搜），返回 `[{teamId, teamName, members, token, currentScore}]` |

- 权限：`RequireRole(Role.Admin)`
- 副作用：创建 `ScoreAdjustment` 记录 + 写入 `GameNotice`（`ScoreAdjustment` 类型）+ 触发 scoreboard 更新
- 0day 加分快捷模板由前端预填 `category=ZeroDayBonus` + 根据级别算 `delta`，后端不区分处理

### 5.2 新增 `BanController`

路由前缀：`api/admin/games/{gameId}/bans`

| Method | Path | 说明 |
|---|---|---|
| GET | `/?teamId=&trackId=` | 封禁列表（可按队/题过滤） |
| POST | `/` | 手动封禁，body: `{ teamId, trackId, level, reason }` |
| DELETE | `/{banId}` | 解封（trap 误触或手动封都可解） |

- 权限：`RequireRole(Role.Admin)`
- 创建 `ChallengeTrackLevelBanState` 记录（`IsManual=true`）
- 解封 = 删除记录（硬删，保留审计在日志里）

### 5.3 `GameController.Submit` 调整

- 提交前检查 `ChallengeTrackLevelBanState`（含手动封）→ 命中则返回 403 + 提示"已被封禁，联系管理员"
- 现有 trap 自动封逻辑保留不动

### 5.4 `EditController.GetGameNotices` 调整

- Player-facing 端点（`/api/game/{id}/notices`）：排除 `NoticeType.RateLimitAlert`
- Admin 端点（`/api/edit/games/{id}/notices`）：返回全部
- 不新增可见性字段，按 type 过滤即可

### 5.5 `RateLimiter` 中间件调整

- `LimitPolicy.Submit` 触发限流时（OnRejected），写入 `GameNotice`：
  - Type: `RateLimitAlert`
  - Values: `[teamName, challengeTitle, DateTime.UtcNow.ToString("o")]`
- 不阻断请求（限流本身已通过 token bucket 减速），只记录告警
- 告警去重：同一队同一题 5 分钟内只记一条（避免告警风暴）

### 5.6 `FlagImportService` 调整

- 导入时将 `levelCols[i].columnName` 写入 `ChallengeLevelConfig.Title`
- 不影响现有数据（旧数据的 Title 为空，前端回退到 `L{n}`）

### 5.7 `EditController` 比赛编辑

- `UpdateGame` 接受 `ZeroDayInfoMarkdown` 字段
- `GetGame` 返回该字段（管理端编辑用）
- Player-facing `GameDetailModel` 也返回该字段（0day tab 渲染用）

---

## 6. 前端变更（按 feature）

### F1: 0day 提交说明 tab

**新增文件：**
- `pages/games/[id]/ZeroDayInfo.tsx`：渲染 markdown（复用 GZCTF 现有 markdown 渲染组件，如 `MarkdownRender` 或 `ChallengeModal` 里的描述渲染逻辑）
  - 从 `game.zeroDayInfoMarkdown` 取内容
  - 模板内占位符 `{{teamToken}}` `{{teamName}}` 用当前用户所在队伍信息替换
  - 文案顶部展示"0day 漏洞提交说明"
  - 文案内含 bugbounty 链接 `https://bugbounty.openharmony.cn/bugbounty/leak`（可点击跳转）

**修改文件：**
- `components/WithGameTab.tsx`：新增 tab 项 `0day` → `/games/{id}/ZeroDayInfo`
- `App.tsx`（或路由配置）：新增路由
- 比赛编辑页：新增 `ZeroDayInfoMarkdown` 编辑器（复用现有 markdown 编辑组件）

### F2: 申诉 + 0day 前端入口关闭

**修改文件：**
- `pages/games/[id]/Submissions.tsx`：移除"发起申诉"按钮 + 申诉 modal（代码注释保留，不删除）
- `pages/games/[id]/Index.tsx:227` 附近：移除 0day 工单跳转按钮
- `components/admin/WithGameEditTab.tsx`：移除 0day 审批 tab 项（代码保留）
- 路由配置：保留 `zeroday-tickets` 路由（不删，但导航不暴露）

### F3: 队伍分数管理

**新增文件：**
- `pages/admin/games/[id]/ScoreAdjustment.tsx`：调分主页
  - 顶部搜索栏：队名/成员/Token 模糊搜（输入 300ms 防抖）
  - 队伍列表表格：队名、成员、Token、当前分、操作（"调整分数" + "0day 加分"按钮）
  - "调整分数"弹窗：delta（±）、reason、category（Manual 默认）
  - "0day 加分"弹窗：severity 下拉（Critical/High/Medium/Low）→ 自动算 delta（`base × 倍数`，base 从 `game.zeroDayBaseScore` 取，默认 1；倍数见附录 B）、CVE 编号、理由（自动生成"0day 奖励 - {CVE} - {severity}"）
- `pages/admin/games/[id]/ScoreLedger.tsx`：调分记录账本
  - 表格：时间、队伍、±分值、理由、类别、操作人
  - 过滤：类别（全部/手动/0day）、队伍
- `pages/admin/games/[id]/ZeroDayBonusLedger.tsx`：0day 加分记录
  - ScoreLedger 的 0day 专属视图（复用组件，固定 category 过滤）
  - 额外显示：CVE 编号、级别
  - 顶部统计：累计 0day 加分、按级别分布

**修改文件：**
- `components/admin/WithGameEditTab.tsx`：新增 tab 项"分数管理" + "调分记录" + "0day 加分记录"
- `Api.ts`：新增 `scoreAdjustment` API 模块（create/list/listZeroDayBonus/searchTeams）

### F4: 封禁/告警管理

**新增文件：**
- `pages/admin/games/[id]/Bans.tsx`：封禁管理
  - 表格：队伍、题目、Level、封禁原因、封禁时间、类型（自动/手动）、操作（解封按钮）
  - 过滤：队伍、题目、类型
  - "手动封禁"弹窗：选队伍 + 选题目 + 选 Level + 填理由
  - trap 自动封记录也在此显示（`IsManual=false`），可解封

**修改文件：**
- `components/admin/WithGameEditTab.tsx`：新增 tab 项"封禁管理"
- `pages/admin/games/[id]/Notices.tsx`：限流告警 type 在通知列表里显示（带特殊样式/图标），管理员可见
- `Api.ts`：新增 `ban` API 模块（list/create/delete）

### F5: Flag 批量操作

**修改文件：**
- `components/admin/FlagEditPanel.tsx`：
  - 每张卡片左上角加 Checkbox
  - 顶部加批量操作栏（选中数 > 0 时显示）：启用、禁用、删除按钮
  - 选中状态用 `Set<string>`（flagId 集合）管理
  - 批量操作调 `api.flags.batchUpdate` / `api.flags.batchDelete`
- `Api.ts`：新增 `batchUpdateFlags(ids[], isEnabled)` 和 `batchDeleteFlags(ids[])`
- 后端 `FlagController`（或 `EditController`）新增批量端点

### F6: Level 名字显示

**修改文件：**
- `components/ChallengeModal.tsx`：`MultiLevelProgress` 渲染 `L{n} - {Title}`（Title 从 `TrackLevelStatusModel.Title` 取）
- `pages/games/[id]/Submissions.tsx`：提交记录里 level 也显示 Title
- 后端 `ChallengeDetailModel` / `SubmissionRecordDto` 已有 Title 字段，确保填充

---

## 7. 国际化（i18n）

### 7.1 新增 key 命名规范

- 命名空间按现有惯例：admin 用 `admin.*`，game 用 `game.*`
- 新增 key 示例：
  - `game.tab.zeroday_info` = "0day 提交" / "0day Submission"
  - `game.zeroday_info.title` = "0day 漏洞提交说明" / "0day Vulnerability Submission Guide"
  - `admin.tab.score_adjustment` = "分数管理" / "Score Management"
  - `admin.tab.score_ledger` = "调分记录" / "Score Ledger"
  - `admin.tab.zeroday_bonus_ledger` = "0day 加分记录" / "0day Bonus Ledger"
  - `admin.tab.bans` = "封禁管理" / "Ban Management"
  - `admin.score.adjust` = "调整分数" / "Adjust Score"
  - `admin.score.zero_day_bonus` = "0day 加分" / "0day Bonus"
  - `admin.ban.manual_ban` = "手动封禁" / "Manual Ban"
  - `admin.ban.unban` = "解封" / "Unban"
  - `admin.flag.batch_enable` = "批量启用" / "Batch Enable"
  - `admin.flag.batch_disable` = "批量禁用" / "Batch Disable"
  - `admin.flag.batch_delete` = "批量删除" / "Batch Delete"

### 7.2 文件位置

- `src/GZCTF/ClientApp/src/locales/zh-CN/admin.json` + `en-US/admin.json`
- `src/GZCTF/ClientApp/src/locales/zh-CN/game.json` + `en-US/game.json`
- 其他 locale 不强制，回退 en-US

### 7.3 禁止硬编码

- 所有新增组件文案必须走 `t()` 函数
- 不允许在 JSX 里写死中文或英文字符串

---

## 8. 实现顺序

按依赖关系排，每个 feature 完整走一遍测试循环：

| 顺序 | Feature | 依赖 | 说明 |
|---|---|---|---|
| F1 | 0day 提交说明 tab | 无 | 后端加 Game 字段 + 前端新 tab + markdown 渲染 |
| F2 | 申诉 + 0day 前端入口关闭 | 无 | 纯前端隐藏，独立 |
| F3 | 队伍分数管理 | F1（Game 字段变更模式参考） | 后端新表 + 新 Controller + 前端 3 页 |
| F4 | 封禁/告警 | F3（搜索能力可复用） | 后端扩展 BanState + RateLimiter + 前端管理页 |
| F5 | Flag 批量操作 | 无 | 后端批量端点 + 前端多选 UI |
| F6 | Level 名字显示 | 无 | 导入写 Title + 前端渲染 |

每个 feature 内部流程：
1. 后端实现（Model + Migration + Controller + Service）
2. 后端接口测试：curl / dotnet test 验证每个 endpoint
3. 前端实现（页面 + 组件 + i18n）
4. Chrome e2e 验证：每种情况完整走一遍，截图 + trace 保存
5. 列 issue 并提交（GitHub Issues 或本地 issue 列表）
6. 修复
7. 复测（完整重跑测试计划）

---

## 9. 测试原则（概要）

> 详细测试计划文档在所有 feature 实现完成后单独编写，基于本文档需求 + 比赛规章制度

- **先后端后前端**：每个 feature 先用 curl/dotnet test 验证后端接口，再测前端
- **Chrome e2e**：用 Playwright（`drive.js` helper）驱动真实 Chrome，每种情况完整验证
- **截图 + trace**：关键步骤截图保存到 `trace-shots/`，trace 保存到 `trace-shots/trace/`
- **Issue 列表**：发现的问题列为 issue 并提交，修复后勾销
- **复测**：修复后按完整测试计划重跑，确保不回归

测试覆盖维度（每个 feature）：
- 正常路径（happy path）
- 权限（未登录、普通用户、管理员）
- 边界（空数据、超长输入、并发）
- 异常（网络错误、后端 500、前端渲染失败）
- i18n（zh-CN + en-US 切换）

---

## 10. 不在本次范围

- 0day 工单后端代码删除（保留，仅前端隐藏）
- 申诉机制后端代码删除（保留，仅前端隐藏）
- 其他 9 个 locale 的翻译（只做 zh-CN + en-US）
- 比赛规章制度的限流参数调优（保留现有 token bucket 配置）
- bugbounty 平台对接（只跳转，不集成）
- 0day 加分的自动化（管理员手动触发，不自动从 bugbounty 拉取）
- 队伍分数的实时缓存优化（实时计算，不做缓存层）

---

## 附录 A：ZeroDayInfoMarkdown 默认模板

```markdown
# 0day 漏洞提交说明

本比赛采用 OpenHarmony 官方 bugbounty 平台接收 0day 漏洞提交。

## 提交流程

1. 前往 [OpenHarmony Bugbounty 平台](https://bugbounty.openharmony.cn/bugbounty/leak)
2. 在平台上提交漏洞报告
3. 提交时请附上你的战队信息：
   - **战队名称**：{{teamName}}
   - **Team Token**：`{{teamToken}}`
4. 平台审核通过后，管理员会根据漏洞级别（Critical/High/Medium/Low）在本平台为你战队加分

## 奖励规则

| 级别 | 加分 |
|---|---|
| Critical | 基础分 × 100 |
| High | 基础分 × 60 |
| Medium | 基础分 × 30 |
| Low | 基础分 × 10 |

## 注意事项

- 0day 漏洞必须针对 OpenHarmony 相关组件
- 提交前请确认漏洞未被公开披露
- 加分由管理员手动操作，如有疑问请联系管理员
```

英文模板同步内置为参考（管理员可手动替换字段内容为英文，不做 per-locale 自动切换）。

---

## 附录 B：级别→分值阶梯（0day 加分）

源自 `ZeroDayTicket.cs:34`：

| Severity | 倍数 | 示例（base=1） |
|---|---|---|
| Critical | ×100 | 100 |
| High | ×60 | 60 |
| Medium | ×30 | 30 |
| Low | ×10 | 10 |

- `base` 从 game 配置取（新增 `Game.ZeroDayBaseScore`，默认 1）
- 前端"0day 加分"弹窗选 severity 后自动计算 `delta = base × 倍数`，管理员可手动覆盖

---

## 附录 C：关键文件清单

### 后端新增
- `src/GZCTF/Models/Data/ScoreAdjustment.cs`
- `src/GZCTF/Controllers/ScoreAdjustmentController.cs`
- `src/GZCTF/Controllers/BanController.cs`
- `src/GZCTF/Migrations/{timestamp}_Iteration2.cs`

### 后端修改
- `src/GZCTF/Models/Data/Game.cs`（+ZeroDayInfoMarkdown, +ZeroDayBaseScore）
- `src/GZCTF/Models/Data/GameNotice.cs`（NoticeType 新增 2 值）
- `src/GZCTF/Models/Data/ChallengeTrackLevelBanState.cs`（+Reason, +BannedBy, +IsManual）
- `src/GZCTF/Models/Data/ChallengeLevelConfig.cs`（Title 启用，无 schema 变更）
- `src/GZCTF/Models/AppDbContext.cs`（+DbSet<ScoreAdjustment>）
- `src/GZCTF/Controllers/EditController.cs`（GetGameNotices 过滤 + Game 编辑加字段）
- `src/GZCTF/Controllers/GameController.cs`（Submit 检查手动封 + GameDetailModel 加字段）
- `src/GZCTF/Middlewares/RateLimiter.cs`（OnRejected 写 GameNotice）
- `src/GZCTF/Services/Import/FlagImportService.cs`（导入写 Title）
- `src/GZCTF/Services/Scoring/ScoringService.cs`（应用 ScoreAdjustment）

### 前端新增
- `src/GZCTF/ClientApp/src/pages/games/[id]/ZeroDayInfo.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/ScoreAdjustment.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/ScoreLedger.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/ZeroDayBonusLedger.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/Bans.tsx`

### 前端修改
- `src/GZCTF/ClientApp/src/components/WithGameTab.tsx`（+0day info tab）
- `src/GZCTF/ClientApp/src/components/admin/WithGameEditTab.tsx`（-0day 审批 tab, +分数/调分/0day记录/封禁 tab）
- `src/GZCTF/ClientApp/src/components/admin/FlagEditPanel.tsx`（多选 + 批量操作栏）
- `src/GZCTF/ClientApp/src/components/ChallengeModal.tsx`（Level 显示 Title）
- `src/GZCTF/ClientApp/src/pages/games/[id]/Index.tsx`（-0day 按钮）
- `src/GZCTF/ClientApp/src/pages/games/[id]/Submissions.tsx`（-申诉按钮）
- `src/GZCTF/ClientApp/src/Api.ts`（+scoreAdjustment / ban / batchFlag 模块）
- `src/GZCTF/ClientApp/src/locales/zh-CN/{admin,game}.json`
- `src/GZCTF/ClientApp/src/locales/en-US/{admin,game}.json`
