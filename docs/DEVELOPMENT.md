# GZCTF Flag Scoring Module — Development Document

## 1. 概述

Flag Scoring Module 是 GZCTF v1.8.3 的扩展模块，在不修改原有表结构和 API 路由的前提下，提供多 flag 独立计分、错误提交罚分、工单申诉、大屏事件播报等功能。

### 1.1 核心约束

| # | 约束 | 说明 |
|---|------|------|
| 1 | 不修改 GZCTF 原有任何表的字段定义 | 所有扩展数据存在新表中（**注：FlagContext 移除阶段例外，见 §1.3**） |
| 2 | 不删除 GZCTF 原有任何 API 路由 | 新增功能走 `/api/ext/` 前缀的新路由 |
| 3 | 新表与旧表的关联仅通过外键引用 | `ChallengeId`、`UserId`、`SubmissionId` |
| 4 | 提交流程通过 DI 替换 | `ScoringService` 注入 `GameController.Submit()`，同步处理，绕过异步 FlagChecker |
| 5 | 所有可变阈值通过配置管理 | `CompetitionSettings` 数据库表 + 管理界面，修改即时生效无需重启 |

### 1.3 FlagContext 移除（v1.8.4）

Flag Scoring Module 最初设计为附加模块，保留原有 `FlagContexts` 表不变。v1.8.4 完成了旧系统的彻底移除：

- **删除 `FlagContexts` 表**：旧 flag 数据迁移至 `ChallengeFlags`，原有功能由新表承接
- **`GameInstances.FlagId` / `ExerciseInstances.FlagId`**：列类型由 `int?` 改为 `Guid?`，外键指向 `ChallengeFlags.Id`
- **`Instance.FlagContext` 导航属性**：名称保留以最小化代码差异，类型由 `FlagContext?` 改为 `ChallengeFlag?`
- **`ScoringService` 统一处理 FirstSolves**：血滴奖励（FirstBlood/SecondBlood/ThirdBlood）由 `ScoringService.HandleCorrectSubmission()` 同步创建，不再依赖异步 `FlagChecker`

迁移文件：`Migrations/20260423100000_RemoveFlagContexts.cs`

### 1.2 模块划分

```
GZCTF Core (不改)
    │
    ├── FlagScoringModule     ← 多 flag 独立计分 + 扣分
    ├── TicketModule          ← 工单申诉
    ├── AdminReviewModule     ← 管理员审核
    └── BigScreenModule       ← 大屏事件播报
```

---

## 2. 数据模型

所有新增表位于 `Models/Data/`，通过 EF Core Fluent API 在 `AppDbContext.OnModelCreating()` 中配置约束。

### 2.1 ChallengeFlags — 多 Flag 配置

每道题可配置多个独立 flag，每个 flag 拥有独立分值。选手提交任意一个 flag 即可获得对应分数。同时承接原 `FlagContexts` 表的功能（flag 分配、附件关联）。

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `ChallengeId` | int | FK → GameChallenges.Id, NOT NULL | 所属题目 |
| `FlagValue` | text | NOT NULL, MaxLength(127) | flag 值 |
| `Score` | int | NOT NULL, [Range(1, int.MaxValue)] | 该 flag 的独立分值 |
| `SortOrder` | int | DEFAULT 0 | 排序 |
| `IsOccupied` | bool | NOT NULL, DEFAULT false | 是否已分配给参赛者（动态附件分发） |
| `AttachmentId` | int? | FK → Attachments.Id, NULLABLE | 每个 flag 独立附件 |
| `ExerciseId` | int? | FK → ExerciseChallenges.Id, NULLABLE | 所属练习题 |
| `CreatedAt` | datetime | NOT NULL | |
| `UpdatedAt` | datetime | NOT NULL | |

**索引与约束**：
- `INDEX(ChallengeId)` — 按题目查询 flag 列表
- `UNIQUE(ChallengeId, FlagValue)` — 同一题目下 flag 值唯一
- `INDEX(AttachmentId)` — 按附件查询
- `INDEX(ExerciseId)` — 按练习题查询

### 2.2 ChallengePenalty — 扣分配置

每道题一条扣分规则。0 表示不扣分。

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `ChallengeId` | int | FK → GameChallenges.Id, UNIQUE | 一题一条配置 |
| `PenaltyScore` | int | NOT NULL, [Range(0, int.MaxValue)] | 每次错误提交扣分值 |
| `CreatedAt` | datetime | NOT NULL | |
| `UpdatedAt` | datetime | NOT NULL | |

**索引与约束**：
- `UNIQUE INDEX(ChallengeId)` — 一道题只有一个扣分规则

### 2.3 SubmissionRecords — 提交记录扩展

补充 GZCTF 原有 `Submissions` 表，记录每次提交的分值变化。

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `SubmissionId` | int | FK → Submissions.Id, UNIQUE | 关联原始提交 |
| `MatchedFlagId` | GUID | FK → ChallengeFlags.Id, NULLABLE | 命中的 flag（NULL=未命中） |
| `ScoreChange` | int | NOT NULL | 本次提交的分值变化（正=加分，负=扣分） |
| `IsPenalty` | bool | NOT NULL, DEFAULT false | 是否为扣分提交 |
| `CreatedAt` | datetime | NOT NULL | |

**索引与约束**：
- `UNIQUE INDEX(SubmissionId)` — 一次提交对应一条扩展记录
- `INDEX(IsPenalty) WHERE IsPenalty = true` — 部分索引，加速工单查询
- `CK_SubmissionRecord_Penalty_NullFlag` — `IsPenalty = true` 时 `MatchedFlagId` 必须为 NULL
- `CK_SubmissionRecord_NonPenalty_RequiresFlag` — `IsPenalty = false` 时 `ScoreChange >= 0`（工单返还/补偿记录 `MatchedFlagId` 可为 NULL，重复提交 flag 记录 `ScoreChange = 0`）

### 2.4 Tickets — 工单

选手对错误提交发起申诉，管理员审核。

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `SubmissionId` | int | FK → Submissions.Id, UNIQUE | 关联的错误提交 |
| `UserId` | GUID | FK → Users.Id, NOT NULL | 发起人 |
| `Description` | text | NOT NULL, MinLength(1) | 申诉描述 |
| `Status` | enum | NOT NULL, DEFAULT 'Pending' | Pending / Approved / Rejected |
| `ReviewerId` | GUID | FK → Users.Id, NULLABLE | 审核人 |
| `ReviewComment` | text | NULLABLE, MaxLength(500) | 审核意见 |
| `RefundScore` | int | DEFAULT 0 | 扣分返还（审批通过时） |
| `BonusScore` | int | DEFAULT 0 | 额外补偿分（审批通过时） |
| `CreatedAt` | datetime | NOT NULL | |
| `ReviewedAt` | datetime | NULLABLE | |

**索引与约束**：
- `UNIQUE INDEX(SubmissionId)` — 一次错误提交只能发起一个工单
- `INDEX(UserId)` — 按选手查询工单
- `INDEX(Status)` — 按状态筛选工单
- `CK_Ticket_Pending_Nulls` — `Status = 'Pending'` 时 `ReviewerId`、`ReviewComment`、`ReviewedAt` 必须为 NULL，`RefundScore`、`BonusScore` 必须为 0
- `CK_Ticket_Resolved_Reviewer` — `Status IN ('Approved', 'Rejected')` 时 `ReviewerId`、`ReviewedAt` 必须 NOT NULL
- `CK_Ticket_Rejected_Comment` — `Status = 'Rejected'` 时 `ReviewComment` 必须 NOT NULL
- `CK_Ticket_Approved_Scores` — `Status = 'Approved'` 时 `RefundScore >= 0` 且 `BonusScore >= 0`

**状态机**：`Pending → Approved | Rejected`（不可逆）

### 2.5 BigScreenEvents — 大屏事件

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `EventType` | enum | NOT NULL, MaxLength(30) | Bonus |
| `ChallengeId` | int | FK → GameChallenges.Id, NOT NULL | |
| `UserId` | GUID | FK → Users.Id, NULLABLE | 相关选手（bonus 类型可为空） |
| `TeamName` | text | NULLABLE, MaxLength(50) | 队伍名 |
| `ChallengeName` | text | NULLABLE, MaxLength(100) | 题目名 |
| `ScoreChange` | int | NULLABLE | 分数变化 |
| `Message` | text | NOT NULL | 播报文案 |
| `IsConsumed` | bool | DEFAULT false | 是否已被大屏消费 |
| `CreatedAt` | datetime | NOT NULL | |

**索引**：`INDEX(IsConsumed, CreatedAt)` — 大屏拉取未消费事件

### 2.6 CompetitionSettings — 比赛设置

键值对存储，值以 JSON 序列化。读取时合并默认值。

| 列 | 类型 | 约束 | 说明 |
|---|---|---|---|
| `Id` | GUID | PK | |
| `Key` | text | NOT NULL, MaxLength(100), UNIQUE | 设置键 |
| `Value` | text | NOT NULL | JSON 值 |
| `UpdatedAt` | datetime | NOT NULL | |

### 2.7 默认配置项

| 键 | 默认值 | 说明 |
|----|--------|------|
| `Scoring.MinScore` | `-100` | 选手总分下限（负值允许未正确提交时产生扣分） |
| `Scoring.DefaultPenaltyScore` | `0` | 新建题目的默认扣分值 |
| `Scoring.MaxPenaltyPerSubmission` | `100` | 单次提交扣分上限 |
| `Ticket.EnableAutoRefund` | `true` | 审核通过时自动计算返还分 |
| `Ticket.DefaultRefundRatio` | `1.0` | 默认返还比例（1.0 = 全额返还） |
| `Ticket.MaxTicketsPerSubmission` | `1` | 单次提交最多发起工单数 |
| `BigScreen.EnableBonusEvents` | `true` | 启用加分事件播报 |

---

## 3. 提交流程

### 3.1 流程概述

```
选手提交 flag
    │
    ▼
GameController.Submit()
    │
    ├── 开启数据库事务
    ├── 写入 Submissions 表（Status = Accepted/WrongAnswer，由 ScoringService 决定）
    ├── ScoringService.ProcessSubmission()
    │       │
    │       ├── 遍历 ChallengeFlags，Ordinal 比较
    │       │
    │       ├── 命中 → HandleCorrectSubmission()
    │       │   ├── 检查是否已提交同一 flag（HasCorrectSubmission）
    │       │   │   ├── 是 → 返回 WrongAnswer + "already submitted" 消息
    │       │   │   └── 否 → 返回 Accepted + flag 分值
    │       │
    │       └── 未命中 → HandleWrongSubmission()
    │           ├── 获取 ChallengePenalty
    │           ├── 读取 MinScore、MaxPenaltyPerSubmission 配置
    │           ├── 计算实际扣分 = MIN(penalty.PenaltyScore, maxPenalty)
    │           ├── 计算组合总分 = GZCTF 基础分 + SubmissionRecords 汇总分
    │           ├── 确保组合总分不低于 MinScore
    │           └── 返回 WrongAnswer + 负分
    │
    ├── 写入 SubmissionRecords（ScoreChange、IsPenalty、MatchedFlagId）
    ├── 命中且非重复提交 → 创建 FirstSolves 记录（血滴奖励逻辑）
    ├── 提交事务
    └── 返回 { submission.Id, scoreResult.Message }
```

### 3.2 FlagChecker 旁路

GZCTF 原有 `FlagChecker` 是异步 `IHostedService`，通过 Channel 处理提交。FlagContext 移除后，`FlagChecker` 内部也改用 `ChallengeFlags` 表进行 flag 匹配。

**旁路机制**：
- 新流程在 `GameController.Submit()` 中同步完成 flag 匹配，直接设置 `Submission.Status = Accepted/WrongAnswer`
- 不向 FlagChecker Channel 写入数据
- FlagChecker 防御性检查：每个提交处理前通过 `ISubmissionRecordRepository.ExistsBySubmissionId()` 检查是否已有 `SubmissionRecord`，有则跳过

### 3.5 FirstSolves 与血滴奖励

FlagContext 移除后，`ScoringService.HandleCorrectSubmission()` 统一负责 FirstSolves 创建：

- **时间窗口**：比赛开始后 1 小时内可产生血滴
- **截止日期**：超过题目截止日期的提交不产生血滴
- **禁用血滴**：`DisableBloodBonus = true` 的题目不产生血滴
- **分区权限**：根据分区配置检查队伍是否有血滴权限
- **提交记录关联**：`FirstSolves.SubmissionId` 关联到具体提交，`UpdateFirstSolveSubmissionId()` 在提交 ID 确认后更新
- **血滴通知**：血滴产生后通过 `GameNotice` 向全比赛广播

### 3.3 边界情况处理

| 场景 | 行为 |
|------|------|
| 重复提交同一 flag | 返回 `WrongAnswer` + "This flag has already been submitted" |
| 罚分后总分低于 MinScore | 扣分截断至 MinScore 底线 |
| 题目未配置扣分 | `HandleWrongSubmission` 返回 `ScoreChange = 0` |
| 题目未配置 flag | `ProcessSubmission` 直接进入 `HandleWrongSubmission` |
| 扣分 > MaxPenaltyPerSubmission | 按 MaxPenaltyPerSubmission 截断 |
| 组合总分已低于 MinScore | 不再扣分（`actualPenalty = 0`） |

### 3.4 积分榜集成

`GameRepository.GenScoreboard()` 第 5.5 步：在排序前查询所有参赛队伍的 `SubmissionRecords.ScoreChange` 总和，累加到 `ScoreboardItem.Score`。扣分和工单返还/补偿正确反映在积分榜上。

```
选手总分 = GZCTF 原始计分（动态分值）+ SUM(SubmissionRecords.ScoreChange)
```

积分榜缓存 7 天滑动过期。以下操作会主动刷新积分榜缓存：

- `GameController.Submit()` — 提交答案后刷新
- `TicketController.Approve()` — 工单审批通过后刷新
- `EditController` — 题目/flag 编辑相关操作后刷新

---

## 4. 工单流程

### 4.1 流程

```
选手查看自己的提交记录
    │
    ├── 每条 IsPenalty=true 的记录旁有「发起申诉」按钮
    │
    ▼
POST /api/ext/tickets
    ├── 校验：MaxTicketsPerSubmission（默认 1）
    ├── 校验：SubmissionRecord 存在且 IsPenalty = true
    ├── 写入 Tickets 表（Status = Pending）
    │
    ▼
管理员审核队列（/admin/games/:id/tickets）
    │
    ├── 通过 (PUT /api/ext/tickets/{id}/approve)
    │   ├── 自动计算返还分（EnableAutoRefund=true 时）
    │   │   └── RefundScore = ROUND(ABS(ScoreChange) × DefaultRefundRatio)
    │   ├── 管理员可手动调整 RefundScore 和 BonusScore
    │   ├── 更新 Ticket 状态 → Approved
    │   ├── ScoringService.ApproveTicket()
    │   │   ├── 创建新 Submission（Status=Accepted）
    │   │   ├── 创建 SubmissionRecord（ScoreChange = RefundScore + BonusScore）
    │   │   ├── 创建 BigScreenEvent（EventType=Bonus）
    │   │   └── SignalR 推送到 BigScreen 组
    │   └── 选手总分 += RefundScore + BonusScore
    │
    └── 驳回 (PUT /api/ext/tickets/{id}/reject)
        ├── ReviewComment 必填
        └── 更新 Ticket 状态 → Rejected（不产生分数变化）
```

### 4.2 约束

| 约束 | 实现方式 |
|------|----------|
| 一次错误提交只能发起一个工单 | `UNIQUE INDEX(SubmissionId)` + `MaxTicketsPerSubmission` 校验 |
| 只有 `IsPenalty=true` 的提交可发起工单 | `TicketController.Create()` 校验 |
| 工单状态流转不可逆 | 数据库 CHECK 约束 + Controller 校验 |
| 驳回必须填理由 | 数据库 CHECK 约束 + Controller 校验 |
| 通过时 RefundScore ≥ 0 | 数据库 CHECK 约束 + Controller 校验 |

---

## 5. API 路由

### 5.1 Flag 管理（Admin）

| 方法 | 路由 | 说明 |
|------|------|------|
| `GET` | `/api/ext/challenges/{challengeId}/flags` | 获取题目的所有 flag |
| `POST` | `/api/ext/challenges/{challengeId}/flags` | 创建 flag |
| `PUT` | `/api/ext/challenges/{challengeId}/flags/{flagId}` | 更新 flag |
| `DELETE` | `/api/ext/challenges/{challengeId}/flags/{flagId}` | 删除 flag（有提交记录的不可删） |

### 5.2 扣分配置（Admin）

| 方法 | 路由 | 说明 |
|------|------|------|
| `GET` | `/api/ext/challenges/{challengeId}/penalty` | 获取扣分配置（不存在时自动创建，使用默认值） |
| `PUT` | `/api/ext/challenges/{challengeId}/penalty` | 更新扣分配置 |

### 5.3 比赛设置（Admin）

| 方法 | 路由 | 说明 |
|------|------|------|
| `GET` | `/api/ext/settings` | 获取所有设置（合并默认值） |
| `PUT` | `/api/ext/settings` | 批量更新设置 |

### 5.4 提交记录

| 方法 | 路由 | 权限 | 说明 |
|------|------|------|------|
| `GET` | `/api/ext/submissions/mine` | User | 当前用户的提交记录 |
| `GET` | `/api/ext/submissions/user/{userId}` | Admin | 指定用户的提交记录 |

### 5.5 工单

| 方法 | 路由 | 权限 | 说明 |
|------|------|------|------|
| `POST` | `/api/ext/tickets` | User | 创建工单 |
| `GET` | `/api/ext/tickets/mine` | User | 我的工单列表 |
| `GET` | `/api/ext/tickets/{ticketId}` | User | 工单详情（仅本人或 Admin） |
| `GET` | `/api/ext/tickets` | Admin | 全部工单（支持 Status 筛选） |
| `PUT` | `/api/ext/tickets/{ticketId}/approve` | Admin | 审批通过 |
| `PUT` | `/api/ext/tickets/{ticketId}/reject` | Admin | 驳回 |

### 5.6 大屏事件

| 方法 | 路由 | 权限 | 说明 |
|------|------|------|------|
| `GET` | `/api/ext/events` | Admin | 获取事件列表 |
| SignalR | `/api/ext/events/stream` | Public | 实时事件推送（BigScreenHub） |

---

## 6. 前端架构

### 6.1 页面路由

| 路由 | 页面 | 说明 |
|------|------|------|
| `/Submissions` | `Submissions.tsx` | 选手全局提交记录 + 发起工单 |
| `/Tickets` | `Tickets.tsx` | 选手工单列表 |
| `/games/{id}/Submissions` | `games/[id]/Submissions.tsx` | 游戏内提交记录（WithGameTab） |
| `/admin/ScoringSettings` | `admin/ScoringSettings.tsx` | 比赛计分设置 |
| `/admin/games/{id}/Tickets` | `admin/games/[id]/Tickets.tsx` | 工单审核（WithGameEditTab） |

### 6.2 组件

| 组件 | 说明 | 嵌入位置 |
|------|------|----------|
| `FlagEditor.tsx` | Flag CRUD 表格 + 弹窗 | 题目编辑页 `challenges/[chalId]/Index.tsx` |
| `PenaltyConfig.tsx` | 扣分配置表单 | 题目编辑页 `challenges/[chalId]/Index.tsx` |

### 6.3 导航入口

| 入口 | 标签 | 路由 |
|------|------|------|
| `WithAdminTab` | Scoring（计分设置） | `/admin/scoringsettings` |
| `WithGameEditTab` | Tickets（工单审核） | `/admin/games/{id}/tickets` |
| `WithGameTab` | Submissions（提交记录） | `/games/{id}/submissions` |

### 6.4 API 封装

所有扩展 API 封装在 `ClientApp/src/utils/ExtApi.ts`，使用 `fetch` 调用 `/api/ext/` 端点。导出类型化的接口和函数。

### 6.5 翻译

新增翻译键分布在 `admin.json` 和 `game.json`，覆盖英文（en-US）和中文（zh-CN）。

**admin.json 新增**：
- `flag.*` — Flag 管理组件（add, edit, column, confirm_delete, empty_value, invalid_score, no_flags, title, total_score）
- `penalty.*` — 扣分配置组件（description, score, score_desc, title）
- `scoring.*` — 计分设置页面（title, section, min_score, default_penalty, max_penalty, auto_refund, refund_ratio, max_tickets, enable_bonus 及各 desc）
- `ticket.*` — 工单审核页面（title, approve, approve_title, reject, reject_reason, refund_score, bonus_score, comment, empty, col.*）
- `tab.games.tickets`, `tab.scoring` — 导航标签

**game.json 新增**：
- `button.create_ticket` — 发起申诉按钮
- `content.no_submissions` — 空状态提示
- `content.submission.*` — 提交记录表格列（time, challenge, score_change, ticket）
- `content.ticket.*` — 工单弹窗（create_title, description, my_tickets, refund_bonus）
- `tab.submissions` — 提交记录标签

---

## 7. 架构设计

### 7.1 依赖注入

所有服务通过 `ServicesExtension.AddCustomServices()` 注册为 Scoped：

```
IChallengeFlagRepository       → ChallengeFlagRepository
IChallengePenaltyRepository    → ChallengePenaltyRepository
ISubmissionRecordRepository    → SubmissionRecordRepository
ITicketRepository              → TicketRepository
IBigScreenEventRepository      → BigScreenEventRepository
ICompetitionSettingRepository  → CompetitionSettingRepository
ScoringService                 → ScoringService
CompetitionSettingsService     → CompetitionSettingsService
```

SignalR Hub 映射（`AppExtensions.UseMiddlewares()`）：

```
BigScreenHub → /api/ext/events/stream
```

### 7.2 Repository 模式

所有仓库继承 `RepositoryBase(AppDbContext context)`，实现对应的接口。遵循 GZCTF 原有的 Repository 模式。

### 7.3 数据完整性

- **数据库层**：CHECK 约束、UNIQUE 索引、外键 + 级联删除策略
- **应用层**：Controller 校验（参数验证、权限检查、状态机校验）
- **并发安全**：提交处理在数据库事务内完成

### 7.4 提交返回值

`GameController.Submit()` 返回 `{ id: number, message?: string }` 而非原始 `number`。

- `id`：提交 ID，用于前端轮询状态
- `message`：后端提示消息（如 "This flag has already been submitted"），前端在错误通知中优先显示

---

## 8. 部署

### 8.1 Docker Compose（生产）

```bash
cp .env.example .env
# 编辑 .env，设置 DB_PASSWORD 和 XOR_KEY
docker compose up -d
```

### 8.2 服务

| 服务 | 端口 | 说明 |
|------|------|------|
| gzctf | 8080 | ASP.NET Core 应用（多阶段构建） |
| db | 5432（内部） | PostgreSQL 17 + 健康检查 |
| redis | 6379（内部） | Redis 7 缓存 |

### 8.3 环境变量

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `DB_PASSWORD` | PostgreSQL 密码 | `gzctf_pass` |
| `XOR_KEY` | 敏感数据加密密钥 | `gzctf-xor-key` |

### 8.4 数据库迁移

EF Core 迁移在应用启动时自动执行（`PrelaunchHelper.MigrateAsync()`），无需手动操作。

---

## 9. 安全性

| 措施 | 说明 |
|------|------|
| Flag 比较 | `StringComparison.Ordinal`，不依赖早期退出优化 |
| 权限控制 | 所有新增 API 使用 `[RequireAdmin]` 或 `[RequireUser]` 属性 |
| SQL 注入防护 | EF Core 参数化查询 |
| 分数下限 | 扣分计算确保组合总分不低于 `MinScore` 配置值 |
| 重复提交防护 | `UNIQUE(SubmissionId)` 防止同一提交重复计分 |
| 工单状态机 | 不可逆（Pending → Approved/Rejected），数据库层 CHECK 约束强制执行 |
| 敏感数据加密 | XOR 密钥通过环境变量注入 |

---

## 10. 文件清单

### 新增文件（27 个）

**数据模型（6）**：
- `Models/Data/ChallengeFlag.cs`
- `Models/Data/ChallengePenalty.cs`
- `Models/Data/SubmissionRecord.cs`
- `Models/Data/Ticket.cs`
- `Models/Data/BigScreenEvent.cs`
- `Models/Data/CompetitionSetting.cs`

**仓库层（12 = 6 接口 + 6 实现）**：
- `Repositories/Interface/IChallengeFlagRepository.cs`
- `Repositories/Interface/IChallengePenaltyRepository.cs`
- `Repositories/Interface/ISubmissionRecordRepository.cs`
- `Repositories/Interface/ITicketRepository.cs`
- `Repositories/Interface/IBigScreenEventRepository.cs`
- `Repositories/Interface/ICompetitionSettingRepository.cs`
- `Repositories/ChallengeFlagRepository.cs`
- `Repositories/ChallengePenaltyRepository.cs`
- `Repositories/SubmissionRecordRepository.cs`
- `Repositories/TicketRepository.cs`
- `Repositories/BigScreenEventRepository.cs`
- `Repositories/CompetitionSettingRepository.cs`

**服务层（2）**：
- `Services/Scoring/ScoringService.cs`
- `Services/Scoring/CompetitionSettingsService.cs`

**控制器（3）**：
- `Controllers/ExtController.cs`
- `Controllers/ExtSubmissionController.cs`
- `Controllers/TicketController.cs`

**SignalR（2）**：
- `Hubs/BigScreenHub.cs`
- `Hubs/Clients/IBigScreenClient.cs`

**前端（9）**：
- `ClientApp/src/utils/ExtApi.ts`
- `ClientApp/src/pages/Submissions.tsx`
- `ClientApp/src/pages/Tickets.tsx`
- `ClientApp/src/pages/admin/ScoringSettings.tsx`
- `ClientApp/src/pages/admin/games/[id]/Tickets.tsx`
- `ClientApp/src/pages/games/[id]/Submissions.tsx`
- `ClientApp/src/components/admin/FlagEditor.tsx`
- `ClientApp/src/components/admin/PenaltyConfig.tsx`

**部署（5）**：
- `Dockerfile`
- `docker-compose.yml`
- `.env.example`
- `.dockerignore`
- `docs/DEPLOYMENT.md`

### 修改文件（16 个）

- `Models/AppDbContext.cs` — 新增 6 个 DbSet + Fluent API 配置
- `Models/Data/Submission.cs` — 新增 `SubmissionRecord?` 反向导航属性
- `Models/Data/Ticket.cs` — `TicketStatus` 枚举添加 `[JsonStringEnumConverter]` 以支持字符串序列化
- `Models/Data/BigScreenEvent.cs` — `BigScreenEventType` 枚举添加 `[JsonStringEnumConverter]`
- `Controllers/GameController.cs` — `Submit()` 集成 ScoringService，返回 `{id, message}`，提交后刷新积分榜缓存
- `Controllers/TicketController.cs` — 注入 `CacheHelper`，审批通过后刷新积分榜缓存
- `Services/FlagChecker.cs` — 防御性跳过已处理提交
- `Repositories/GameRepository.cs` — `GenScoreboard()` 集成 SubmissionRecords 分数
- `Repositories/TicketRepository.cs` — 修复导航属性 Include（GameChallenge、SubmissionRecord、Reviewer），改用 `FindAsync` 数组参数
- `Extensions/Startup/ServicesExtension.cs` — 注册 8 个新服务/仓库
- `Extensions/Startup/AppExtensions.cs` — 映射 BigScreenHub
- `ClientApp/src/Api.ts` — `gameSubmit` 返回类型改为 `{id, message}`
- `ClientApp/src/components/GameChallengeModal.tsx` — `submitMessage` 状态 + 显示后端消息
- `ClientApp/src/components/WithGameTab.tsx` — 新增 Submissions 标签
- `ClientApp/src/components/admin/WithAdminTab.tsx` — 新增 Scoring 标签
- `ClientApp/src/components/admin/WithGameEditTab.tsx` — 新增 Tickets 标签
- `ClientApp/src/pages/admin/games/[id]/challenges/[chalId]/Index.tsx` — 嵌入 FlagEditor + PenaltyConfig
- `ClientApp/src/pages/admin/games/[id]/Tickets.tsx` — 工单审核页：可点击行展开详情弹窗，含完整描述、审核意见、退还分、加分选择，通过/驳回按钮
- `ClientApp/src/pages/Tickets.tsx` — 选手工单页：可点击行查看详情，已审核工单展示审核结果
- `ClientApp/src/utils/Shared.tsx` — 新增 `showSuccessMsg`
- `ClientApp/src/locales/en-US/admin.json` — 新增翻译键（ticket.*, scoring.*, flag.*, penalty.*）
- `ClientApp/src/locales/en-US/game.json` — 新增翻译键（ticket.*, submission.*）
- `ClientApp/src/locales/zh-CN/admin.json` — 新增翻译键
- `ClientApp/src/locales/zh-CN/game.json` — 新增翻译键
- `Migrations/20260421103728_AddFlagScoringModule.cs` — EF Core 迁移（Flag Scoring Module 初始化）
- `Migrations/20260423100000_RemoveFlagContexts.cs` — EF Core 迁移（移除 FlagContexts 表，统一 flag 系统）
- `Migrations/AppDbContextModelSnapshot.cs` — 迁移快照

### 删除文件（1 个）

- `Models/Data/FlagContext.cs` — 旧 flag 上下文实体（已被 ChallengeFlag 替代）

---

## 11. 开发环境

### 11.1 前置条件

- .NET SDK 10.0
- Node.js 20+ with pnpm
- Docker（PostgreSQL、Redis）

### 11.2 启动

```bash
# 1. 启动基础设施
docker run -d --name gzctf-postgres \
  -e POSTGRES_USER=gzctf -e POSTGRES_PASSWORD=gzctf_pass \
  -e POSTGRES_DB=gzctf -p 5432:5432 postgres:17-alpine
docker run -d --name gzctf-redis -p 6379:6379 redis:7-alpine

# 2. 后端
cd src/GZCTF
dotnet ef database update
dotnet run

# 3. 前端
cd src/GZCTF/ClientApp
pnpm install
pnpm dev
```

后端 `http://localhost:8080`，前端开发服务器 `http://localhost:5173`。

### 11.3 已验证的测试场景

以下场景通过端到端测试验证：

| # | 场景 | 验证点 |
|---|------|--------|
| 1 | 正确提交 flag | Accepted 状态，ScoreChange 为正 |
| 2 | 错误提交 flag | WrongAnswer 状态，产生罚分记录（IsPenalty=true） |
| 3 | 罚分不超上限 | 实际扣分 ≤ MaxPenaltyPerSubmission |
| 4 | 总分不低于下限 | 组合总分截断至 MinScore（-100） |
| 5 | 重复提交同一 flag | 返回 "already submitted" 消息 |
| 6 | 发起工单 | 仅 IsPenalty=true 的提交可发起 |
| 7 | 工单审批通过 | 返还分 + 加分写入 SubmissionRecord，积分榜更新 |
| 8 | 工单驳回 | 必须填写驳回理由，不产生分数变化 |
| 9 | 积分榜缓存刷新 | 提交和工单审批后积分榜实时更新 |
| 10 | 比赛设置 | 管理界面修改即时生效，无需重启 |
| 11 | Flag CRUD | 创建、编辑、删除 flag，有提交记录的不可删 |
| 12 | 扣分配置 | 新题目自动使用 DefaultPenaltyScore |

### 11.4 已验证的 Bug 修复

| 问题 | 原因 | 修复 |
|------|------|------|
| Admin 修改 flag 后积分榜不更新 | 双系统不同步：EditController 用 FlagContexts，ExtController 用 ChallengeFlags | 移除 FlagContexts，统一使用 ChallengeFlags |
| 血滴奖励（Blood Bonus）缺失 | ScoringService 未创建 FirstSolves 记录 | HandleCorrectSubmission 中实现完整血滴逻辑 |
| 积分榜不更新（提交后） | `GameController.Submit()` 未刷新积分榜缓存 | 提交后调用 `CacheHelper.FlushScoreboardCache()` |
| 积分榜不更新（工单审批后） | `TicketController.Approve()` 未刷新缓存 | 注入 `CacheHelper`，审批后刷新 |
| 前端工单状态显示为数字 | `TicketStatus` 枚举序列化为 0/1/2 整数 | 添加 `[JsonStringEnumConverter]` |
| 管理员工单列表无题目名 | `GetAllTickets` 未 Include GameChallenge | 添加 `.ThenInclude(s => s.GameChallenge)` |
| 选手工单列表 penaltyScore=0 | `GetUserTickets` 未 Include SubmissionRecord | 添加 `.ThenInclude(s => s.SubmissionRecord)` |
| 大屏事件类型显示异常 | `BigScreenEventType` 枚举未配置 JSON 转换器 | 添加 `[JsonStringEnumConverter]` |
| 设置 API 读取整数值失败 | `GetInt32()` 对 `1.0` 抛出 `FormatException` | 改用 `TryGetInt32` + `GetDouble` 回退 |

### 11.5 验证新表

```bash
docker compose exec db psql -U gzctf -d gzctf -c "\dt"
```

预期新表：`ChallengeFlags`、`ChallengePenalty`、`SubmissionRecords`、`Tickets`、`BigScreenEvents`、`CompetitionSettings`。

预期已删除表：`FlagContexts`（数据已迁移至 `ChallengeFlags`）。
