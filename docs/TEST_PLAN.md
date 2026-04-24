# GZCTF Flag Scoring Module — 前端验收测试文档

## 测试环境

| 组件 | 地址 | 说明 |
|------|------|------|
| 前端 | `http://localhost:8080` | 生产构建，API 同源 |
| 后端 | `http://localhost:8080` | ASP.NET Core 10 |
| 数据库 | `localhost:5432` | PostgreSQL 17 |
| 缓存 | `localhost:6379` | Redis 7 |

### 测试账号准备

| 角色 | 账号 | 用途 |
|------|------|------|
| Admin | 管理员账号 | 配置计分、审核工单、管理 flag |
| User A | 选手 A（队伍 Alpha） | 提交 flag、发起工单 |
| User B | 选手 B（队伍 Beta） | 交叉验证积分榜 |

### 测试前置条件

1. 管理员创建一场比赛（Game），添加至少 2 道题目（Challenge A、Challenge B）
2. 两支队伍（Alpha、Beta）报名并通过审核
3. 比赛状态为 `Started`（已开始）

---

## 一、Flag 管理（Admin）—— 数据链路：DB → API → FlagEditor 组件

### 链路说明

```
数据库 ChallengeFlags 表
  → GET /api/ext/challenges/{challengeId}/flags
  → ExtApi.getFlags()
  → FlagEditor 组件 Table 渲染
```

### 测试场景

#### F-01: 查看空 Flag 列表

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 以 Admin 登录，进入比赛 → 题目编辑页 | 左侧题目列表可见 |
| 2 | 点击一个尚未配置 flag 的题目 | FlagEditor 区域显示 "暂无 Flag 配置，点击添加 Flag 创建"（灰色文字） |
| 3 | 检查总分 Badge | 显示 "总分: 0" |

**验收标准**：空状态文案正确，总分显示为 0。

#### F-02: 创建 Flag

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击 "添加 Flag" 按钮 | 弹出 Modal，标题为 "添加 Flag" |
| 2 | 输入 Flag 值 `flag{test_alpha}` | TextInput 接受输入 |
| 3 | 设置分值为 `200` | NumberInput 显示 200 |
| 4 | 排序保持默认 `0` | NumberInput 显示 0 |
| 5 | 点击 "保存" | Modal 关闭，成功通知 "保存成功" |
| 6 | 观察 Flag 列表 | 新增一行：`flag{test_alpha}` \| `+200` \| `0` |
| 7 | 观察总分 Badge | 更新为 "总分: 200" |

**验收标准**：创建成功后列表即时刷新，总分累加正确。

#### F-03: 创建 Flag 表单校验

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Flag 值留空，点击保存 | 通知 "Flag 值不能为空" |
| 2 | 分值为 0，点击保存 | 通知 "分值必须大于等于 1" |
| 3 | 分值为 -5，点击保存 | 通知 "分值必须大于等于 1" |

**验收标准**：前端校验生效，不发送无效请求。NumberInput 的 min={1} 阻止输入 ≤0 的值。

#### F-04: 创建重复 Flag（后端校验）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 创建与已有 flag 相同值的 flag | 错误通知 "This flag value already exists for this challenge" |

**验收标准**：后端唯一约束生效，错误消息展示正确。

#### F-05: 编辑 Flag

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击某 Flag 行的编辑图标（铅笔） | Modal 打开，标题 "编辑 Flag"，表单预填当前值 |
| 2 | 修改分值为 `300` | NumberInput 显示 300 |
| 3 | 点击保存 | Modal 关闭，列表更新，对应行显示 `+300` |
| 4 | 观察总分 Badge | 重新计算为所有 flag 分值之和 |

**验收标准**：编辑预填正确，保存后即时刷新。

#### F-06: 删除 Flag（无提交记录）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击某 Flag 行的删除图标（红色垃圾桶） | 浏览器 confirm 弹窗 "确定删除此 Flag 吗？" |
| 2 | 点击 "确定" | 成功通知 "删除成功"，该行消失 |
| 3 | 观察总分 Badge | 更新为剩余 flag 总分 |

**验收标准**：确认弹窗文案正确，删除后列表即时刷新。

#### F-07: 删除 Flag（有提交记录 — 后端校验）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 尝试删除已有选手提交过的 flag | 错误通知 "Cannot delete flag with existing submissions" |

**验收标准**：后端拒绝删除，前端展示错误消息。Flag 列表中该行仍然存在。

#### F-08: Flag 值超长校验

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 输入超过 126 字符的 flag 值并保存 | 错误通知 "Flag value is too long" |

**验收标准**：后端 `MaxLength(126)` 校验生效。

---

## 二、扣分配置（Admin）—— 数据链路：DB → API → PenaltyConfig 组件

### 链路说明

```
数据库 ChallengePenalty 表
  → GET /api/ext/challenges/{challengeId}/penalty（不存在时自动创建，使用 Scoring.DefaultPenaltyScore 默认值）
  → ExtApi.getPenalty()
  → PenaltyConfig 组件 NumberInput
```

### 测试场景

#### P-01: 查看新题目默认扣分

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 以 Admin 登录，进入新题目的编辑页 | PenaltyConfig 区域显示 NumberInput 值为 0（默认 DefaultPenaltyScore） |
| 2 | 检查标题和描述 | 标题 "扣分配置"，描述 "每次错误提交扣分值，0 表示不扣分" |

**验收标准**：首次访问自动创建 penalty 记录，使用默认值 0。

#### P-02: 修改扣分值

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 在 PenaltyConfig NumberInput 中输入 `10` | 值更新为 10 |
| 2 | 点击保存按钮 | 成功通知 "保存成功" |
| 3 | 刷新页面 | NumberInput 仍然显示 10（持久化成功） |

**验收标准**：保存后值持久化。NumberInput min={0} 阻止输入负数。

#### P-03: 扣分值设为 0（禁用扣分）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 将扣分值改为 `0`，点击保存 | 保存成功 |
| 2 | 后续选手错误提交此题目 | 不产生扣分记录 |

**验收标准**：0 表示禁用扣分，描述文案 "设为 0 则禁用扣分" 正确。

---

## 三、比赛计分设置（Admin）—— 数据链路：DB → API → ScoringSettings 页面

### 链路说明

```
数据库 CompetitionSettings 表
  → GET /api/ext/settings（合并 CompetitionSettingsService.Defaults）
  → ExtApi.getSettings()
  → ScoringSettings 页面各表单控件

用户修改 → PUT /api/ext/settings
  → CompetitionSettingsService.Set()
  → 即时生效，无需重启
```

### 测试场景

#### S-01: 查看默认设置值

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 点击导航栏 "计分设置" | 进入 `/admin/scoringsettings` 页面 |
| 2 | 检查各字段默认值 | 见下方默认值表 |

| 字段 | 预期默认值 | 输入范围 |
|------|-----------|---------|
| 最低分（Min Score） | **-100** | -10000 ~ 0 |
| 默认扣分（Default Penalty） | 0 | 0 ~ 1000 |
| 单次扣分上限（Max Penalty） | 100 | 0 ~ 1000 |
| 自动返还（Auto Refund） | **开启**（Switch ON） | — |
| 默认返还比例（Refund Ratio） | 1.0 | 0 ~ 2.0，步长 0.1 |
| 单次提交工单上限（Max Tickets） | 1 | 1 ~ 10 |
| 启用加分事件（Enable Bonus） | **开启**（Switch ON） | — |

**验收标准**：所有字段默认值与约束文档一致，特别注意 `MinScore` 必须为 -100 而非 0。

#### S-02: 修改最低分并保存

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 将 "最低分" 改为 `-50` | NumberInput 显示 -50 |
| 2 | 点击保存 | 成功通知 "保存成功" |
| 3 | 刷新页面 | 最低分仍为 -50 |

**验收标准**：修改持久化。NumberInput min={-10000} max={0} 阻止输入超出范围的值。

#### S-03: 修改自动返还和比例

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 关闭 "自动返还" Switch | Switch 变为 OFF |
| 2 | 将返还比例改为 `0.5` | NumberInput 显示 0.5（支持一位小数） |
| 3 | 点击保存 | 成功 |
| 4 | 刷新页面 | 两个值均持久化 |

**验收标准**：Switch 和 Float NumberInput 均正确持久化。

#### S-04: 关闭加分事件播报

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 关闭 "启用加分事件" Switch | Switch 变为 OFF |
| 2 | 点击保存 | 成功 |
| 3 | 后续审批通过工单 | 大屏不推送 Bonus 事件 |

**验收标准**：关闭后不产生 BigScreenEvent。

#### S-05: 修改即时生效验证（不重启）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 修改 MaxPenaltyPerSubmission 从 100 改为 50，保存 | 成功 |
| 2 | 选手提交错误 flag（题目扣分配置为 100） | 实际扣分 ≤ 50（被上限截断） |
| 3 | 改回 100 并保存 | 后续扣分恢复 ≤ 100 |

**验收标准**：设置修改后立即对新的提交生效，无需重启服务。

---

## 四、Flag 提交 —— 数据链路：前端提交 → GameController → ScoringService → SubmissionRecord

### 链路说明

```
GameChallengeModal 输入 flag
  → POST /api/game/{gameId}/challenge/{challengeId}/submit
  → GameController.Submit()
    → ScoringService.ProcessSubmission()
      → 匹配 ChallengeFlags（Ordinal 比较）
      → 命中 → HandleCorrectSubmission() → 检查重复 → Accepted + 正分
      → 未命中 → HandleWrongSubmission() → 计算扣分 → WrongAnswer + 负分
    → SubmissionRecords 写入
    → FlushScoreboardCache
  → 返回 { id, message }
  → 前端轮询 gameStatus → 展示 Accepted/WrongAnswer 通知
```

### 测试场景

#### SUB-01: 正确提交 Flag

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 进入比赛，点击题目打开提交弹窗 | ChallengeModal 打开 |
| 2 | 输入正确的 flag 值（如 `flag{test_alpha}`） | 输入框接受 |
| 3 | 点击提交 | 通知 "flag 似乎被正确提交了！"（loading 状态） |
| 4 | 等待轮询结果 | 通知变为绿色 "答案正确"，弹窗自动关闭 |
| 5 | 进入提交记录页面 `/Submissions` | 可见一条记录：题目名 \| `+200`（绿色 Badge）\| 无工单按钮 |

**验收标准**：
- 提交后 loading 通知 → 绿色成功通知
- 弹窗自动关闭（容器类题目同时销毁实例）
- 提交记录中 ScoreChange 为正分（绿色 Badge）
- 罚分记录列（Ticket 列）为空（非 IsPenalty 记录不显示工单按钮）

#### SUB-02: 错误提交 Flag（题目已配置扣分）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 对配置了扣分（如 10）的题目提交错误 flag | 提交成功 |
| 2 | 等待轮询 | 通知变为红色 "答案错误"，优先显示后端 message（如无则随机提示语） |
| 3 | 进入提交记录页面 | 可见一条记录：题目名 \| `-10`（红色 Badge）\| "发起申诉" 按钮 |

**验收标准**：
- WrongAnswer 展示红色通知
- 提交记录中 ScoreChange 为负分（红色 Badge）
- IsPenalty=true 的记录显示 "发起申诉" 按钮

#### SUB-03: 重复提交同一 Flag

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 再次提交已正确提交过的 flag | 提交成功 |
| 2 | 等待轮询 | 通知变为红色，显示 "This flag has already been submitted" |

**验收标准**：后端返回 message，前端在 WrongAnswer 通知中优先显示此消息。

#### SUB-04: 罚分不超过 MaxPenaltyPerSubmission 上限

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 设置题目扣分为 200，MaxPenaltyPerSubmission 为 100 | — |
| 2 | 选手提交错误 flag | 提交记录显示 `-100`（而非 -200） |

**验收标准**：实际扣分 = MIN(题目扣分, MaxPenaltyPerSubmission)。

#### SUB-05: 总分不低于 MinScore 下限

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 确保 MinScore = -100 | — |
| 2 | 选手 A 多次错误提交，使总分趋近 -100 | — |
| 3 | 当总分已达 -100 时再次错误提交 | 提交记录显示 `0`（不再扣分）或扣分截断 |
| 4 | 观察积分榜 | 选手 A 总分不低于 -100 |

**验收标准**：组合总分 = GZCTF 原始分 + SUM(SubmissionRecords.ScoreChange)，截断至 MinScore。

#### SUB-06: 未配置 Flag 的题目

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 对未配置任何 flag 的题目提交任意内容 | 通知红色 "答案错误" |
| 2 | 提交记录 | ScoreChange = 0（无扣分，无加分） |

**验收标准**：未配置 flag 的题目所有提交都走 HandleWrongSubmission，且 penalty 为 null 时不扣分。

#### SUB-07: 扣分配置为 0 的题目错误提交

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 题目扣分为 0，选手提交错误 flag | 通知红色 "答案错误" |
| 2 | 提交记录 | ScoreChange = 0，IsPenalty = false，无 "发起申诉" 按钮 |

**验收标准**：扣分 0 时不产生 IsPenalty 记录，不显示工单按钮。

#### SUB-08: 提交后积分榜缓存刷新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 正确提交 flag（+200 分） | 提交成功 |
| 2 | 立即查看积分榜 | 选手 A 队伍总分包含这 200 分 |
| 3 | 选手 B 错误提交（-10 分） | 提交成功 |
| 4 | 立即查看积分榜 | 选手 B 队伍总分扣减 10 分 |

**验收标准**：提交后积分榜实时更新（缓存被刷新）。

---

## 五、选手提交记录与工单 —— 数据链路：SubmissionRecords → Tickets

### 链路说明

```
GET /api/ext/submissions/mine
  → ExtSubmissionController.GetMySubmissions()
  → SubmissionRecordRepository.GetByUser()
  → 返回 SubmissionRecordDto[]（含 challengeName、scoreChange、isPenalty）

POST /api/ext/tickets
  → TicketController.Create()
  → 校验：IsPenalty=true、MaxTicketsPerSubmission
  → 创建 Ticket（Status=Pending）

GET /api/ext/tickets/mine
  → TicketController.GetMyTickets()
  → 返回 TicketDto[]（含 status、refundScore、bonusScore）
```

### 测试场景

#### T-01: 选手提交记录页面（全局）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 访问 `/Submissions` | 页面标题 "提交记录" |
| 2 | 检查表格列 | 时间 \| 题目 \| 分数变化 \| 工单 |
| 3 | 正确提交的记录 | 绿色 Badge 显示 `+{score}`，工单列为空 |
| 4 | 错误提交的记录（未发起工单） | 红色 Badge 显示 `-{score}`，工单列显示 "发起申诉" 按钮 |
| 5 | 错误提交的记录（已发起工单） | 红色 Badge，工单列显示状态 Badge（黄色 Pending / 绿色 Approved / 红色 Rejected） |

**验收标准**：三条数据链路的 UI 状态正确区分。

#### T-02: 选手游戏内提交记录页面

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 进入比赛 → 点击 "提交记录" Tab | 页面渲染在 WithGameTab 内 |
| 2 | 表格数据 | 与全局页面相同，但限制在当前游戏范围内 |

**验收标准**：WithGameTab 导航标签正确显示 "提交记录"。

#### T-03: 发起工单

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 在提交记录中，点击错误提交行的 "发起申诉" 按钮 | Modal 打开，标题 "发起工单" |
| 2 | 检查 Modal 内容 | 显示题目名 + 扣分 Badge（红色），描述 Textarea |
| 3 | 输入申诉描述 "我认为这个 flag 是正确的，请审核" | Textarea 接受输入 |
| 4 | 点击保存 | Modal 关闭，成功通知 "保存成功" |
| 5 | 检查提交记录列表 | 该行的 "发起申诉" 按钮变为黄色 "Pending" Badge |
| 6 | 点击 Pending Badge | 弹出工单详情 Modal |

**验收标准**：工单创建后按钮状态切换正确，详情 Modal 可查看。

#### T-04: 空描述校验

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 打开工单 Modal，不输入描述，点击保存 | 按钮无响应（Textarea required 阻止空提交） |

**验收标准**：前端 required 校验。

#### T-05: 非 IsPenalty 记录不显示工单按钮

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 查看正确提交（+200）的记录行 | 工单列为空（无按钮、无 Badge） |
| 2 | 查看 ScoreChange=0 的错误提交记录 | 工单列为空 |

**验收标准**：只有 `isPenalty=true` 的记录才显示工单按钮。

#### T-06: 重复发起工单校验

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 对已有 Pending/Approved/Rejected 工单的提交，再次尝试发起 | 后端拒绝（但前端按钮已变为状态 Badge，无法再次发起） |

**验收标准**：前端 UI 已阻止重复发起（按钮变为 Badge），后端 `MaxTicketsPerSubmission` 为双重保障。

#### T-07: 选手工单列表页面

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手 A 访问 `/Tickets` | 页面标题 "我的工单" |
| 2 | 检查表格列 | 题目 \| 申诉描述 \| 状态 \| 时间 \| 返还/补偿 |
| 3 | Pending 工单 | 状态 Badge 黄色 "Pending"，返还列显示 "—" |
| 4 | Approved 工单 | 状态 Badge 绿色 "Approved"，返还列显示 `+{refundScore+bonusScore}`（绿色） |
| 5 | Rejected 工单 | 状态 Badge 红色 "Rejected"，返还列显示驳回意见 |

**验收标准**：三种状态展示正确。

#### T-08: 工单详情 Modal（选手视角）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击 Pending 工单行 | Modal 打开，标题 "工单详情" |
| 2 | 检查信息 | 题目名、状态、提交内容（Code 块）、扣分（红色）、时间、提交编号、申诉描述 |
| 3 | 底部区域 | 显示灰色文字 "工单正在审核中，请等待管理员处理。" |
| 4 | 点击 Approved 工单 | 额外显示审核结果区域：审核人、审核时间、返还分（+绿）、加分（+绿）、审核意见 |
| 5 | 点击 Rejected 工单 | 审核结果区域：审核人、审核时间、审核意见（驳回理由） |

**验收标准**：三种状态的详情 Modal 信息完整，Pending 无审核信息，Approved 有分数信息，Rejected 有驳回理由。

---

## 六、管理员工单审核 —— 数据链路：Tickets → Approve/Reject → SubmissionRecord + BigScreenEvent

### 链路说明

```
GET /api/ext/tickets（Admin）
  → TicketController.GetAllTickets()
  → 支持 Status 筛选

PUT /api/ext/tickets/{id}/approve
  → TicketController.Approve()
    → 自动计算 RefundScore（EnableAutoRefund=true）
    → TicketRepository.ApproveTicket() → Status=Approved
    → ScoringService.ApproveTicket()
      → 创建新 Submission（Accepted）+ SubmissionRecord（ScoreChange = RefundScore + BonusScore）
      → 创建 BigScreenEvent（EventType=Bonus）
      → SignalR → BigScreenHub → 大屏客户端
    → FlushScoreboardCache

PUT /api/ext/tickets/{id}/reject
  → TicketController.Reject()
    → 校验 Comment 必填
    → TicketRepository.RejectTicket() → Status=Rejected
```

### 测试场景

#### A-01: 管理员工单列表

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 进入比赛 → 点击 "工单审核" Tab | 页面标题 "工单管理" |
| 2 | 检查表格列 | 选手 \| 题目 \| 提交内容 \| 申诉描述 \| 状态 \| 时间 |
| 3 | 检查状态筛选下拉框 | All / Pending / Approved / Rejected 四个选项 |
| 4 | 选择 "Pending" | 表格仅显示 Pending 工单 |

**验收标准**：表格列完整，筛选功能正常。

#### A-02: 工单审核 Modal — Pending 状态

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击 Pending 工单行 | Modal 打开，显示工单详情 |
| 2 | 检查信息区域 | 选手名、题目名、提交内容（Code 块）、扣分（红色粗体）、状态 Badge、时间、申诉描述 |
| 3 | 检查审核表单 | 审核意见 Textarea（必填） |
| 4 | 检查自动返还 | 返还分 NumberInput 预填 = 扣分值（EnableAutoRefund=true） |
| 5 | 检查加分 | 额外加分 NumberInput 预填 = 0 |
| 6 | 检查按钮 | "取消"、"驳回"（红色边框）、"通过"（绿色）三个按钮 |

**验收标准**：审核 Modal 包含完整信息 + 可编辑的返还分和加分 + 三个操作按钮。

#### A-03: 审批通过工单

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 对 Pending 工单，保持返还分 = 扣分值，加分 = 0 | — |
| 2 | 输入审核意见 "审核通过"（可选） | Textarea 接受输入 |
| 3 | 点击 "通过" 按钮 | Modal 关闭，成功通知 "保存成功" |
| 4 | 工单列表刷新 | 该工单状态变为绿色 "Approved" |
| 5 | 切换选手账号，查看提交记录 | 新增一条 ScoreChange = 返还分 的记录（绿色 Badge），来源为 Ticket |
| 6 | 查看积分榜 | 该选手队伍总分增加了返还分 |

**验收标准**：
- 审批后工单状态变 Approved
- SubmissionRecord 正确写入（ScoreChange = RefundScore + BonusScore）
- 积分榜实时更新
- 工单详情中 RefundScore ≥ 0、BonusScore ≥ 0

#### A-04: 审批通过工单 — 手动调整返还分

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 将返还分改为 `5`，加分设为 `2` | NumberInput 更新 |
| 2 | 点击通过 | 成功 |
| 3 | 查看提交记录 | ScoreChange = 7（5 + 2） |

**验收标准**：管理员可手动覆盖返还分和加分。

#### A-05: 审批通过工单 — 自动返还关闭

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 在计分设置中关闭 "自动返还" | — |
| 2 | 打开新的 Pending 工单 | 返还分 NumberInput 预填 = 0（不再自动计算） |
| 3 | 管理员手动输入返还分 `8`，点击通过 | 成功 |

**验收标准**：EnableAutoRefund=false 时，返还分默认 0，由管理员手动输入。

#### A-06: 审批通过工单 — BigScreen 事件

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 确保 "启用加分事件" 开启 | — |
| 2 | 审批通过一个工单 | 大屏（BigScreenHub）收到 SignalR 推送的 Bonus 事件 |
| 3 | 检查事件内容 | 包含队伍名、题目名、分数变化、播报文案 |

**验收标准**：审批通过后 BigScreenHub 推送事件，`GET /api/ext/events` 可查到该事件。

#### A-07: 驳回工单

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 打开 Pending 工单 | — |
| 2 | 输入驳回理由 "flag 确实错误，不予返还" | Textarea 接受输入 |
| 3 | 点击 "驳回" 按钮 | Modal 关闭，成功通知 "保存成功" |
| 4 | 工单列表刷新 | 该工单状态变为红色 "Rejected" |
| 5 | 切换选手账号，查看工单详情 | 状态 Rejected，审核意见为驳回理由 |
| 6 | 查看提交记录 | 无新增 SubmissionRecord（不产生分数变化） |
| 7 | 查看积分榜 | 该选手队伍总分不变 |

**验收标准**：
- 驳回后状态变 Rejected
- 不产生新的 SubmissionRecord
- 积分榜不变

#### A-08: 驳回工单 — 未填写理由

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 打开 Pending 工单，不输入审核意见 | — |
| 2 | 点击 "驳回" | 前端通知 "驳回时必须填写审核意见"（阻止请求） |

**验收标准**：前端校验 Comment 必填，阻止发送无效请求。后端同样有校验（双重保障）。

#### A-09: 已审核工单不可再次操作

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击 Approved 或 Rejected 工单 | Modal 显示审核结果（只读），无审核表单和操作按钮 |
| 2 | 显示审核人、审核时间、审核意见 | 信息完整展示 |

**验收标准**：已审核工单只读展示，不可再次审批或驳回。后端也拒绝非 Pending 状态的操作。

#### A-10: 管理员工单详情 Modal（已审核状态）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 点击 Approved 工单 | 显示审核人、审核时间、返还分（+绿）、加分（+绿）、审核意见 |
| 2 | 点击 Rejected 工单 | 显示审核人、审核时间、审核意见 |

**验收标准**：Approved 显示分数详情，Rejected 显示驳回理由。

---

## 七、积分榜集成 —— 数据链路：GZCTF Score + SubmissionRecords

### 链路说明

```
GenScoreboard()
  → GZCTF 原始动态计分
  → + SUM(SubmissionRecords.ScoreChange) per participation
  → ScoreboardItem.Score
  → 缓存到 Redis（7 天滑动过期）
  → 前端积分榜 API 读取
```

### 测试场景

#### SB-01: 正确提交后积分榜更新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 记录当前积分榜各队伍分数 | — |
| 2 | 选手 A 正确提交 flag（+200） | 提交成功 |
| 3 | 刷新积分榜 | 选手 A 队伍总分 = 原分数 + 200 |

**验收标准**：积分榜实时反映 flag 得分。

#### SB-02: 错误提交后积分榜更新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 记录当前积分榜 | — |
| 2 | 选手 B 错误提交（扣分 -10） | 提交成功 |
| 3 | 刷新积分榜 | 选手 B 队伍总分 = 原分数 - 10 |

**验收标准**：积分榜实时反映扣分。

#### SB-03: 工单通过后积分榜更新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 记录当前积分榜 | — |
| 2 | Admin 审批通过工单（返还 +10，加分 +2） | 审批成功 |
| 3 | 刷新积分榜 | 选手总分 = 原分数 + 12 |

**验收标准**：工单返还和加分正确累加到积分榜。

#### SB-04: 工单驳回后积分榜不变

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 记录当前积分榜 | — |
| 2 | Admin 驳回工单 | 驳回成功 |
| 3 | 刷新积分榜 | 选手总分不变 |

**验收标准**：驳回不产生分数变化。

#### SB-05: Flag 编辑后积分榜缓存刷新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 修改某 flag 的分值（100 → 300） | 保存成功 |
| 2 | 刷新积分榜 | 已提交该 flag 的选手分数不受影响（历史分不变），新提交者获得 300 分 |

**验收标准**：flag 编辑操作刷新缓存（通过 ExtController FlushScoreboardForChallenge），但历史提交分数不变。

#### SB-06: Penalty 修改后积分榜缓存刷新

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 修改扣分值（0 → 20） | 保存成功 |
| 2 | 刷新积分榜 | 已有分数不受影响 |
| 3 | 选手错误提交该题目 | 新扣分按 20 计算 |

**验收标准**：penalty 修改刷新缓存，新提交使用新扣分值。

---

## 八、导航与路由 —— 前端可访问性

### 测试场景

#### N-01: 管理员导航标签

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 登录，观察 WithAdminTab | 标签栏包含 "计分设置" |
| 2 | 点击 "计分设置" | 跳转至 `/admin/scoringsettings` |
| 3 | Admin 进入比赛编辑页，观察 WithGameEditTab | 标签栏包含 "工单审核" |
| 4 | 点击 "工单审核" | 跳转至 `/admin/games/{id}/tickets` |

**验收标准**：两个 Admin 导航标签（Scoring、Tickets）正确显示和跳转。

#### N-02: 选手导航标签

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手进入比赛，观察 WithGameTab | 标签栏包含 "提交记录" |
| 2 | 点击 "提交记录" | 跳转至 `/games/{id}/submissions` |

**验收标准**：选手游戏内提交记录标签正确。

#### N-03: 全局路由

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手访问 `/Submissions` | 全局提交记录页面（WithNavBar） |
| 2 | 选手访问 `/Tickets` | 我的工单页面（WithNavBar） |

**验收标准**：两个独立路由可正常访问。

---

## 九、翻译（i18n）—— 中英文切换

### 测试场景

#### I18N-01: 中文环境

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 切换语言为中文 | — |
| 2 | 检查 Flag 管理组件 | "Flag 管理"、"添加 Flag"、"编辑 Flag"、"确定删除此 Flag 吗？"、"暂无 Flag 配置，点击添加 Flag 创建" |
| 3 | 检查扣分配置组件 | "扣分配置"、"每次错误提交扣分值，0 表示不扣分" |
| 4 | 检查计分设置页面 | "比赛计分设置"、"最低分"、"默认扣分"、"自动返还" |
| 5 | 检查工单审核页面 | "工单管理"、"通过"、"驳回"、"审核意见" |
| 6 | 检查选手提交记录 | "提交记录"、"发起申诉"、"分数变化" |
| 7 | 检查选手工单页面 | "我的工单"、"工单正在审核中，请等待管理员处理。" |

#### I18N-02: 英文环境

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 切换语言为英文 | — |
| 2 | 检查 Flag 管理组件 | "Flag Management"、"Add Flag"、"Edit Flag"、"Are you sure you want to delete this flag?" |
| 3 | 检查扣分配置组件 | "Penalty Configuration" |
| 4 | 检查计分设置页面 | "Competition Scoring Settings"、"Minimum Score"、"Default Penalty" |
| 5 | 检查工单审核页面 | "Ticket Management"、"Approve"、"Reject" |
| 6 | 检查选手提交记录 | "Submissions"、"Create Ticket"、"Score Change" |
| 7 | 检查选手工单页面 | "My Tickets"、"Your ticket is pending review." |

**验收标准**：所有新增翻译键在中英文下均正确显示，无 key 名称裸露。

---

## 十、异常与边界场景

### 测试场景

#### E-01: Flag 值超长（>126 字符）

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 创建 flag，输入 127 字符的 flag 值 | 错误通知 "Flag value is too long" |

#### E-02: 扣分设为负数

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 在 PenaltyConfig 中尝试输入负数 | NumberInput min={0} 阻止输入 |

#### E-03: 返还分设为负数

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 在工单审核中尝试输入负数返还分 | NumberInput min={0} 阻止输入 |

#### E-04: 加分设为负数

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 在工单审核中尝试输入负数加分 | NumberInput min={0} 阻止输入 |

#### E-05: 未登录访问

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 未登录直接访问 `/api/ext/submissions/mine` | 返回 401 Unauthorized |
| 2 | 未登录访问 `/Submissions` 页面 | 页面重定向或空白（前端路由守卫） |

#### E-06: 选手访问 Admin API

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 选手直接调用 `/api/ext/challenges/1/flags` | 返回 403 Forbidden |
| 2 | 选手访问 `/admin/scoringsettings` | 前端路由守卫阻止 |

#### E-07: 题目不存在

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 访问 `/api/ext/challenges/99999/flags` | 返回空列表或 404（取决于实现） |

#### E-08: 工单不存在

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 访问 `/api/ext/tickets/{不存在的GUID}` | 返回 404 "Ticket not found" |

#### E-09: 审批非 Pending 工单

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 尝试通过 API 审批已 Approved 的工单 | 返回 400 "Only pending tickets can be approved" |

#### E-10: 大屏事件列表

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 调用 `GET /api/ext/events?unconsumedOnly=true` | 返回未消费的 Bonus 事件列表 |
| 2 | 检查事件字段 | eventType="Bonus", message 包含队伍名和返还分 |

---

## 十一、端到端完整流程测试

### 场景：选手提交 → 扣分 → 申诉 → 管理员审核通过

```
[Admin]  创建题目，添加 flag{secret}（分值 200），设置扣分 10
[User A] 提交错误 flag → WrongAnswer，-10 分
[User A] 查看 /Submissions → 可见 -10 记录 + "发起申诉" 按钮
[User A] 点击发起申诉 → 填写描述 → 提交
[User A] 查看 /Tickets → 可见 Pending 工单
[Admin]  查看 /admin/games/{id}/tickets → 可见 Pending 工单
[Admin]  点击工单 → 返还分自动填 10 → 输入意见 → 点击通过
[User A] 查看 /Tickets → 工单变 Approved，返还 +10
[User A] 查看 /Submissions → 新增 +10 记录
[User A] 查看积分榜 → 总分恢复（-10 + 10 = 0，仅剩 GZCTF 基础分）
```

### 场景：选手提交 → 扣分 → 申诉 → 管理员驳回

```
[Admin]  创建题目，添加 flag{secret}（分值 200），设置扣分 10
[User A] 提交错误 flag → WrongAnswer，-10 分
[User A] 发起工单
[Admin]  点击工单 → 输入驳回理由 "flag 明显错误" → 点击驳回
[User A] 查看 /Tickets → 工单变 Rejected，显示驳回理由
[User A] 查看 /Submissions → 无新增记录
[User A] 查看积分榜 → 总分仍为 -10
```

### 场景：多次提交 + MinScore 下限保护

```
[Admin]  设置 MinScore = -100，题目扣分 = 50
[User A] GZCTF 基础分 = 10（仅提交了一个正确 flag）
[User A] 第 1 次错误提交 → -50，组合总分 = 10 - 50 = -40
[User A] 第 2 次错误提交 → -50，组合总分 = -40 - 50 = -90
[User A] 第 3 次错误提交 → -10（截断至 -100），组合总分 = -100
[User A] 第 4 次错误提交 → 0（不再扣分），组合总分 = -100
```

### 场景：设置修改即时生效

```
[Admin]  修改 DefaultPenaltyScore: 0 → 5
[Admin]  创建新题目 → PenaltyConfig 默认显示 5
[Admin]  修改 MaxPenaltyPerSubmission: 100 → 20
[User A] 对扣分 50 的题目错误提交 → 仅扣 20（上限截断）
[Admin]  关闭 EnableAutoRefund
[Admin]  审核新工单 → 返还分默认 0（不再自动计算）
```

---

## 十二、统一 Flag 系统验证（FlagContext 移除后）

### 链路说明

```
FlagContexts 表已删除。所有 flag 操作统一走 ChallengeFlags 表：

Admin 添加/修改 Flag：
  EditController → GameChallengeRepository → ChallengeFlags 表
  ExtController → ChallengeFlagRepository → ChallengeFlags 表

选手提交 Flag：
  GameController.Submit() → ScoringService.ProcessSubmission()
    → 匹配 ChallengeFlags
    → 命中 → HandleCorrectSubmission()
      → 创建 SubmissionRecord
      → 创建 FirstSolves（血滴奖励）
    → 未命中 → HandleWrongSubmission()
      → 创建 SubmissionRecord（扣分）

积分榜：
  GenScoreboard() → FirstSolves + SubmissionRecords → 最终分数
```

### 测试场景

#### UF-01: Admin 通过 FlagEditor 添加 Flag 后选手可提交

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 在题目编辑页通过 FlagEditor 添加 flag `flag{unified_test}`（分值 150） | 创建成功，列表显示新 flag |
| 2 | 选手提交 `flag{unified_test}` | Accepted，获得 150 分 |
| 3 | 查看积分榜 | 选手总分增加 150 分 |
| 4 | 检查 FirstSolves 表 | 存在对应记录（含 SubmissionId） |

**验收标准**：EditController 添加的 flag 与 ExtController 添加的 flag 行为一致，FirstSolves 正确创建。

#### UF-02: Admin 通过 ExtController 添加 Flag 后选手可提交

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 通过 FlagEditor（调用 ExtController API）添加 flag `flag{ext_test}`（分值 200） | 创建成功 |
| 2 | 选手提交 `flag{ext_test}` | Accepted，获得 200 分 |
| 3 | 查看积分榜 | 选手总分增加 200 分 |

**验收标准**：ExtController 路径与 EditController 路径共用同一 ChallengeFlags 表。

#### UF-03: Admin 修改 Flag 后积分榜同步

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 通过 FlagEditor 修改 flag 分值（150 → 300） | 修改成功 |
| 2 | 积分榜刷新 | 已提交该 flag 的选手分数不变（历史分不变） |
| 3 | 新选手提交该 flag | 获得 300 分 |

**验收标准**：flag 修改操作刷新积分榜缓存，历史分数不受影响。

#### UF-04: Admin 删除 Flag 后选手无法提交

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 删除某 flag | 删除成功 |
| 2 | 选手提交该 flag 值 | WrongAnswer（该 flag 已不存在于 ChallengeFlags） |

**验收标准**：删除操作立即生效，选手无法再通过该 flag 得分。

#### UF-05: 动态容器题目 Flag 模板生成

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 创建动态容器题目，设置 FlagTemplate 为 `flag{[TEAM_HASH]}` | — |
| 2 | 选手 A 启动容器 | GameInstance.FlagContext 指向动态生成的 ChallengeFlag |
| 3 | 选手提交生成的 flag | Accepted，FirstSolves 正确创建 |
| 4 | 检查 GameInstance 表 | FlagId 为 GUID 类型，正确关联到 ChallengeFlags.Id |

**验收标准**：动态 flag 生成后正确写入 ChallengeFlags，Instance 关联正确。

#### UF-06: 动态附件题目 Flag 分发

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | Admin 创建动态附件题目，上传多个带 flag 的附件 | 每个附件关联一个 ChallengeFlag |
| 2 | 选手 A 获取附件 | 分配一个未占用的 ChallengeFlag（IsOccupied=false → true） |
| 3 | 选手 B 获取附件 | 分配另一个未占用的 ChallengeFlag |
| 4 | 选手提交各自的 flag | 各自 Accepted，分数独立 |

**验收标准**：IsOccupied 字段正确用于动态附件分发，不同选手获得不同 flag。

#### UF-07: 血滴奖励（Blood Bonus）验证

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 比赛刚开始，题目未有人提交 | — |
| 2 | 队伍 Alpha 第一个正确提交 flag | FirstBlood 产生，FirstSolves.Type = FirstBlood |
| 3 | 队伍 Beta 第二个正确提交 | SecondBlood 产生 |
| 4 | 队伍 Gamma 第三个正确提交 | ThirdBlood 产生 |
| 5 | 队伍 Delta 第四个正确提交 | Normal solve，无血滴 |
| 6 | 查看积分榜 | Alpha 获得额外血滴加分 |

**验收标准**：血滴奖励由 ScoringService 同步创建，不依赖异步 FlagChecker。

#### UF-08: FlagContexts 表已删除

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 连接数据库，执行 `\dt` | FlagContexts 表不存在 |
| 2 | 执行 `SELECT * FROM "ChallengeFlags" LIMIT 1` | 包含 IsOccupied、AttachmentId、ExerciseId 列 |
| 3 | 执行 `SELECT * FROM "GameInstances" LIMIT 1` | FlagId 列为 uuid 类型 |

**验收标准**：数据库 schema 与迁移一致。

#### UF-09: 旧数据迁移完整性

| 步骤 | 操作 | 预期前端表现 |
|------|------|-------------|
| 1 | 迁移前 FlagContexts 中有 N 条记录 | — |
| 2 | 迁移后检查 ChallengeFlags | 原 FlagContexts 中的 flag 值均存在于 ChallengeFlags |
| 3 | 迁移前 GameInstances.FlagId 指向 FlagContexts.Id | — |
| 4 | 迁移后 GameInstances.FlagId 指向对应 ChallengeFlags.Id（GUID） | 关联关系正确保留 |

**验收标准**：数据迁移完整，无数据丢失。

---

## 附录 A：API 端点速查

| 端点 | 方法 | 权限 | 用途 |
|------|------|------|------|
| `/api/ext/challenges/{id}/flags` | GET | Admin | 获取 flag 列表 |
| `/api/ext/challenges/{id}/flags` | POST | Admin | 创建 flag |
| `/api/ext/challenges/{id}/flags/{fid}` | PUT | Admin | 更新 flag |
| `/api/ext/challenges/{id}/flags/{fid}` | DELETE | Admin | 删除 flag |
| `/api/ext/challenges/{id}/penalty` | GET | Admin | 获取扣分配置 |
| `/api/ext/challenges/{id}/penalty` | PUT | Admin | 更新扣分配置 |
| `/api/ext/settings` | GET | Admin | 获取比赛设置 |
| `/api/ext/settings` | PUT | Admin | 更新比赛设置 |
| `/api/ext/submissions/mine` | GET | User | 我的提交记录 |
| `/api/ext/submissions/user/{uid}` | GET | Admin | 指定用户提交记录 |
| `/api/ext/tickets` | POST | User | 创建工单 |
| `/api/ext/tickets/mine` | GET | User | 我的工单 |
| `/api/ext/tickets/{id}` | GET | User | 工单详情 |
| `/api/ext/tickets` | GET | Admin | 全部工单 |
| `/api/ext/tickets/{id}/approve` | PUT | Admin | 审批通过 |
| `/api/ext/tickets/{id}/reject` | PUT | Admin | 驳回工单 |
| `/api/ext/events` | GET | Admin | 大屏事件列表 |
| `/api/ext/events/stream` | SignalR | Public | 大屏实时推送 |

## 附录 B：数据库约束速查

| 表 | 约束 | 验证方式 |
|----|------|---------|
| ChallengeFlags | FK → Attachments (AttachmentId) | 删除附件时 SetNull |
| ChallengeFlags | FK → ExerciseChallenges (ExerciseId) | 练习题关联 |
| ChallengeFlags | FK → GameChallenges (ChallengeId) | 级联删除 |
| ChallengeFlags | UNIQUE(ChallengeId, FlagValue) | 创建重复 flag 返回错误 |
| ChallengeFlags | INDEX(AttachmentId) | 按附件查询 flag |
| ChallengeFlags | INDEX(ExerciseId) | 按练习题查询 flag |
| ChallengePenalty | UNIQUE(ChallengeId) | 一题只有一条配置 |
| SubmissionRecords | UNIQUE(SubmissionId) | 一次提交只有一条记录 |
| SubmissionRecords | CK_IsPenalty_NullFlag | IsPenalty=true 时 MatchedFlagId 为 NULL |
| SubmissionRecords | CK_NonPenalty_RequiresFlag | IsPenalty=false 时 ScoreChange > 0 |
| Tickets | UNIQUE(SubmissionId) | 一次提交只有一个工单 |
| Tickets | CK_Pending_Nulls | Pending 时 ReviewerId 等为 NULL |
| Tickets | CK_Resolved_Reviewer | Approved/Rejected 时 ReviewerId NOT NULL |
| Tickets | CK_Rejected_Comment | Rejected 时 ReviewComment NOT NULL |
| Tickets | CK_Approved_Scores | Approved 时 RefundScore ≥ 0, BonusScore ≥ 0 |

## 附录 C：测试检查清单

- [ ] F-01 ~ F-08: Flag 管理全部通过
- [ ] P-01 ~ P-03: 扣分配置全部通过
- [ ] S-01 ~ S-05: 计分设置全部通过
- [ ] SUB-01 ~ SUB-08: Flag 提交全部通过
- [ ] T-01 ~ T-08: 选手提交记录与工单全部通过
- [ ] A-01 ~ A-10: 管理员工单审核全部通过
- [ ] SB-01 ~ SB-06: 积分榜集成全部通过
- [ ] N-01 ~ N-03: 导航与路由全部通过
- [ ] I18N-01 ~ I18N-02: 中英文翻译全部通过
- [ ] E-01 ~ E-10: 异常与边界场景全部通过
- [ ] UF-01 ~ UF-09: 统一 Flag 系统验证全部通过
- [ ] 端到端场景 × 4: 全部通过
