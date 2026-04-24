# GZCTF Flag Scoring Module — 测试与合规验证报告

测试日期：2026-04-23
测试依据：`docs/TEST_PLAN.md` + `docs/DEVELOPMENT.md`

---

## 一、约束合规验证结果

对 `docs/DEVELOPMENT.md` 中全部约束逐项验证，共 **147 项**，**147 项全部通过**。

| 章节 | 约束内容 | 项数 | 结果 |
|------|---------|------|------|
| 1.1 | 核心约束（不改表/不删路由/仅FK关联/DI替换/配置管理） | 5 | 全部通过 |
| 2.1 | ChallengeFlags 数据模型（8列+2索引+1唯一约束） | 11 | 全部通过 |
| 2.2 | ChallengePenalty 数据模型（5列+1唯一索引） | 6 | 全部通过 |
| 2.3 | SubmissionRecords 数据模型（6列+2索引+2CHECK） | 10 | 全部通过 |
| 2.4 | Tickets 数据模型（11列+4索引+4CHECK+状态机） | 20 | 全部通过 |
| 2.5 | BigScreenEvents 数据模型（10列+1索引） | 11 | 全部通过 |
| 2.6 | CompetitionSettings 数据模型（4列+1唯一索引） | 5 | 全部通过 |
| 2.7 | 默认配置项（7项） | 7 | 全部通过 |
| 3.3 | 边界情况（8项） | 8 | 全部通过 |
| 4.2 | 工单约束（5项） | 5 | 全部通过 |
| 5 | API 路由（18个端点） | 18 | 全部通过 |
| 6 | 前端架构（5页面+2组件+3导航+API封装+翻译） | 20 | 全部通过 |
| 7.1 | 依赖注入（8个服务） | 8 | 全部通过 |
| 7.2 | SignalR Hub 映射 | 1 | 全部通过 |
| 7.3 | 数据完整性（DB层+应用层+并发安全） | 3 | 全部通过 |
| 7.4 | 提交返回值格式 | 3 | 全部通过 |
| 8 | 部署文件（Dockerfile/compose/.env等） | 6 | 全部通过 |
| 9 | 安全性（7项措施） | 7 | 全部通过 |

---

## 二、发现并修复的问题（3 个，均已修复）

### 问题 1（高）：重复 flag 提交不创建 SubmissionRecord

**根因**：`GameController.Submit()` 第 1038 行条件 `scoreResult.ScoreChange != 0 || scoreResult.IsPenalty` 导致 ScoreChange=0 的提交不创建记录。15/54 条历史提交受影响。

**修复**（4 处）：
1. `ScoringService.cs`：`HandleCorrectSubmission` 重复提交时设置 `MatchedFlagId = matchedFlag.Id`
2. `GameController.cs`：移除条件判断，始终调用 `CreateRecord`
3. `AppDbContext.cs` + 数据库：CHECK 约束 `ScoreChange > 0` → `ScoreChange >= 0`
4. `DEVELOPMENT.md`：同步更新约束描述

### 问题 2（中）：AdminTickets NumberInput 使用 `Number(v)`

**根因**：`AdminTickets.tsx` 第 287、295 行 `refundScore`/`bonusScore` 的 NumberInput 使用 `Number(v)`。

**修复**：改用 `getInputNumber(v)` + NaN 守卫。

### 问题 3（低）：FlagEditor sortOrder NumberInput 使用 `Number(v)`

**根因**：`FlagEditor.tsx` 第 193 行 `sortOrder` 的 NumberInput 使用 `Number(v)`。

**修复**：改用 `getInputNumber(v)` + NaN 守卫。

---

## 三、数据库验证

### 新增表（6 张，全部存在）
ChallengeFlags、ChallengePenalties、SubmissionRecords、Tickets、BigScreenEvents、CompetitionSettings

### CHECK 约束（6 个，全部正确）
- `CK_SubmissionRecord_Penalty_NullFlag` — `IsPenalty = false OR MatchedFlagId IS NULL`
- `CK_SubmissionRecord_NonPenalty_RequiresFlag` — `IsPenalty = true OR ScoreChange >= 0`
- `CK_Ticket_Pending_Nulls` — Pending 时审核字段为 NULL
- `CK_Ticket_Resolved_Reviewer` — 已审核时 ReviewerId/ReviewedAt NOT NULL
- `CK_Ticket_Rejected_Comment` — Rejected 时 ReviewComment NOT NULL
- `CK_Ticket_Approved_Scores` — Approved 时 RefundScore/BonusScore >= 0

### 外键（9 个，全部正确）
级联删除策略：SubmissionRecords/Tickets/BigScreenEvents 对 Submissions/GameChallenges 使用 CASCADE，MatchedFlagId 使用 SET NULL

### 索引（20 个，含主键，全部正确）

---

## 四、API 测试结果

| 端点 | 方法 | 测试结果 |
|------|------|---------|
| `/api/ext/challenges/{id}/flags` | GET/POST/PUT/DELETE | 全部通过 |
| `/api/ext/challenges/{id}/penalty` | GET/PUT | 全部通过 |
| `/api/ext/settings` | GET/PUT | 全部通过 |
| `/api/ext/submissions/mine` | GET | 全部通过 |
| `/api/ext/submissions/user/{uid}` | GET | 全部通过 |
| `/api/ext/tickets` | POST/GET | 全部通过 |
| `/api/ext/tickets/mine` | GET | 全部通过 |
| `/api/ext/tickets/{id}` | GET | 全部通过 |
| `/api/ext/tickets/{id}/approve` | PUT | 全部通过 |
| `/api/ext/tickets/{id}/reject` | PUT | 全部通过 |
| `/api/ext/events` | GET | 全部通过 |

---

## 五、前端验证

- 5 个页面路由全部可访问（Submissions、Tickets、ScoringSettings、AdminTickets、GameSubmissions）
- 2 个嵌入组件（FlagEditor、PenaltyConfig）正确渲染
- 3 个导航标签（WithAdminTab/WithGameEditTab/WithGameTab）正确注册
- zh-CN/en-US 翻译键全部完整（admin.json 6 组、game.json 3 组）
- 提交返回 `{id, message}` 格式正确，前端优先显示后端消息

---

## 六、总结

- **约束合规**：147/147 全部通过
- **测试场景**：63 个场景，62 个首次通过，1 个发现问题后修复
- **代码问题**：3 个（1 后端 + 2 前端），全部已修复并重新部署
- **文档问题**：1 处（MaxFlagLength 126→127），已修正
