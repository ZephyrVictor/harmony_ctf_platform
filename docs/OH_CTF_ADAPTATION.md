# GZCTF OpenHarmony CTF 平台 — 功能说明与适配文档

> 本文档详细说明基于 GZCTF 二次开发后的 OpenHarmony CTF 平台的**完整功能**,
> 以及为适配 **第二届 OpenHarmony CTF (Capture The Flaw) 智能漏洞挖掘赛** 所做的所有改动。
>
> - **基线版本**: GZCTF (上游开源版)
> - **适配目标**: 比赛规则 R1–R30 (`memo/比赛规则-第二届OpenHarmony CTF.docx`)
> - **数据集**: `memo/OH社区第二届CTF大赛AI数据集（0608）修复.xlsx` (59 行 / 14 仓库 / 22 挑战)
> - **测试审计**: `trace.md` (Phase 0–8 完整执行日志)

---

## 目录

1. [平台概览](#1-平台概览)
2. [原 GZCTF 基础功能](#2-原-gzctf-基础功能)
3. [OpenHarmony CTF 适配新增功能](#3-openharmony-ctf-适配新增功能)
4. [数据模型变更](#4-数据模型变更)
5. [API 端点变更](#5-api-端点变更)
6. [前端 UI 变更](#6-前端-ui-变更)
7. [核心业务流程](#7-核心业务流程)
8. [规则符合性矩阵 (R1–R30)](#8-规则符合性矩阵-r1r30)
9. [部署与配置](#9-部署与配置)
10. [已知限制与未实现项](#10-已知限制与未实现项)

---

## 1. 平台概览

### 1.1 技术栈

| 层 | 技术 |
|---|---|
| 后端 | ASP.NET Core (.NET 10) + EF Core + PostgreSQL + Redis |
| 前端 | React 18 + TypeScript + Vite + Mantine UI v7 |
| 容器 | Docker Compose (gzctf + db + redis) |
| 实时通信 | SignalR (比赛通知、提交监控) |
| 文件存储 | 本地文件系统 / 对象存储 (MinIO/S3) |
| 容器调度 | Docker 或 K8s (动态容器题目) |

### 1.2 部署架构

```
┌─────────────────────────────────────────┐
│  Docker Compose (harmony_ctf_platform)  │
│  ┌──────────┐  ┌─────┐  ┌────────────┐  │
│  │ gzctf    │←→│redis│  │ postgresql │  │
│  │ :8081    │  │     │  │ db         │  │
│  └──────────┘  └─────┘  └────────────┘  │
│       ↑                                  │
│       └─── Playwright daemon (Chrome)    │
└─────────────────────────────────────────┘
```

### 1.3 用户角色

| 角色 | 权限 |
|---|---|
| **Admin (管理员)** | 平台全局管理 + 比赛管理 + 0day 工单审核 (对应 R2 "评审组") |
| **Monitor (裁判)** | 比赛监控、提交记录查看 |
| **User (选手)** | 注册、建队/加入队伍、提交 flag、提交 0day 工单 |

---

## 2. 原 GZCTF 基础功能

以下功能继承自上游 GZCTF,本适配**未做修改**:

### 2.1 题目类型

- **静态附件**: 共用附件,任意 flag 均可提交
- **动态附件**: 按队伍分发 flag 和附件
- **静态容器**: 共用容器模版,不下发 flag
- **动态容器**: 自动生成,每队 flag 唯一

### 2.2 动态分值

$$f(S, r, d, x) = \left \lfloor S \times \left[r + (1-r) \times \exp\left( \dfrac{1 - x}{d} \right) \right] \right \rfloor$$

- `S` = 原始分值
- `r` = 最低分值比例
- `d` = 难度系数
- `x` = 提交次数

**三血奖励**: 一血 5%、二血 3%、三血 1% 当前题目分值。

### 2.3 比赛管理

- 创建/编辑/隐藏比赛
- 题目增删、批量导入、分组管理
- 队伍审核、邀请码、注册邮箱限制
- Writeup 收集、查阅、批量下载
- 比赛监控、提交记录、事件日志
- 排行榜导出、提交记录导出

### 2.4 用户与权限

- 用户封禁、三级权限管理 (Admin/Monitor/User)
- SMTP 邮件验证
- Cloudflare Turnstile 恶意注册防护
- API Token 管理

### 2.5 基础设施

- 基于 SignalR 的实时通知
- 题目流量 TCP over WebSocket 代理转发
- 流量捕获 (可选)
- Redis 集群缓存
- 暗黑模式、多国语言、自定义主题
- 网站页脚、图标、HTML 描述自定义

---

## 3. OpenHarmony CTF 适配新增功能

为适配 OpenHarmony CTF 比赛规则 (R1–R30),平台新增以下 7 大功能模块:

### 3.1 多级 Flag 链 (Track + Level 模型) — R6/R7/R8

#### 3.1.1 数据模型

每个题目 (GameChallenge) 下可有**多个 Track (漏洞链)**,每个 Track 包含 **3 个 Level 的 Flag**:

```
GameChallenge (题目)
  └─ ChallengeTrack (漏洞链)
       ├─ ChallengeFlag (Level 1: file_path:line_number, 5 分)
       ├─ ChallengeFlag (Level 2: commit_hash, 4 分)
       └─ ChallengeFlag (Level 3: CWE, 1 分)
```

#### 3.1.2 关键字段 (ChallengeFlag)

| 字段 | 类型 | 说明 |
|---|---|---|
| `Level` | int | 1/2/3 (对应 5/4/1 分) |
| `TrackId` | Guid | 所属漏洞链 ID |
| `FlagValue` | string | 正确答案 |
| `AnswerType` | enum | `Exact` / `LineRange` / `MultiLine` / `SingleLine` |
| `IsVisible` | bool | 是否在 UI 展示 (用于"可见 Flag"列表) |
| `IsEnabled` | bool | 是否启用 |

#### 3.1.3 答案类型与 L1 格式

- **Exact**: 精确匹配 (L2 commit hash、L3 CWE)
- **SingleLine**: 单行格式 `path.cpp:c:1060`
- **LineRange**: 范围格式 `path.cpp:c:550-559`
- **MultiLine**: 多行格式 `path.cpp:c:281、c:286`

**R30 组合 CWE 顺序敏感性**: L3 答案 `CWE-125; CWE-787` 与 `CWE-787; CWE-125` 不同 (Exact 类型强制顺序)。

#### 3.1.4 前端 UI

题目弹窗新增 **MultiLevelProgress** 组件:
- 进度条: `已解 X 个漏洞` + `当前: L1/L2/L3`
- Track 选择器: Badge 列表 (TRACK 1/2/3... + T1/T3 等 0day 创建的 track + "+ New Vulnerability")
- Level 进度: `L1 ✓ → L2 (当前) → L3 ⏸`
- 可见 Flag 列表: 展示所有可见 flag 的状态

### 3.2 0day 工单系统 — R15/R16/R17/R18/R24

#### 3.2.1 工单提交入口

与 Nday flag 提交**完全分离**:
- **Nday 提交**: `/games/{id}/challenges` → 题目弹窗 → flag 输入框
- **0day 提交**: `/games/{id}/zeroday-tickets` → "提交 0day 工单" 按钮 → 工单表单

#### 3.2.2 工单数据模型 (ZeroDayTicket)

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | Guid | 工单 ID |
| `Title` | string? | 标题 (可选) |
| `Description` | string | 漏洞描述 (必填) |
| `Status` | enum | `Pending` / `Approved` / `Rejected` |
| `Severity` | enum | `None`/`Low`/`Medium`/`High`/`Critical` (R17) |
| `Module` | string? | 模块名 (e.g. `ecmascript/base`) |
| `FilePath` | string? | 文件路径 (L1 flag key) |
| `LineNumber` | string? | 行号 (e.g. `c:1060`, `c:550-559`) |
| `CommitHash` | string? | 修复 commit hash (40-char hex) |
| `CWE` | string? | CWE 分类 (e.g. `CWE-787` 或 `CWE-125; CWE-787`) |
| `PoC` | string? | 漏洞证明 |
| `BonusScore` | int | 0day 奖励分 (R17) |
| `ReviewComment` | string? | 审核意见 |
| `CreatedTrackId` | Guid? | 审核通过后创建的 Track ID (R18 闭环) |
| `ChallengeId` | int? | 关联题目 ID (可选,用于 bonus 加分) |
| `GameId` | int | 所属比赛 |
| `UserId` | Guid | 提交者 |
| `ReviewerId` | Guid? | 审核者 (Admin) |

#### 3.2.3 R17 严重度加分

| Severity | BonusScore |
|---|---|
| Critical | 100 × N |
| High | 60 × N |
| Medium | 30 × N |
| Low | 10 × N |
| None | 0 |

(N = 对应数量,默认 N=1)

#### 3.2.4 R18 0day→Nday 闭环

Admin 审核通过 0day 工单时:
1. 设置 `Status = Approved`
2. 根据 `FilePath` + `LineNumber` + `CommitHash` + `CWE` 自动创建新 `ChallengeTrack`
   - `Category = "0day"` (区分于数据集 Nday)
   - `SortOrder = 9999` (排在最后)
3. 自动创建 3 个 `ChallengeFlag` (L1=FilePath:LineNumber, L2=CommitHash, L3=CWE)
4. 将 `CreatedTrackId` 写回工单
5. 后续所有队伍可解此新 Track,作为 Nday 得分 (5+4+1=10 分)

#### 3.2.5 R24 跨队重复判定

- **0day 跨队拒绝**: 同一 `FilePath` + `LineNumber` 的 0day 工单,只有第一个被 Approve,后续 Approve 抛出异常:
  > `R24: This 0day vulnerability has already been approved for another team. Subsequent submitters can only score as Nday, no 0day bonus.`
- **Nday 多队独立**: 0day Approve 后创建的 Track,所有队伍都可独立解,各队都得分 (R24 修正后语义)

### 3.3 Excel 批量导入 — R4/R28

#### 3.3.1 导入流程

1. Admin 上传 `.xlsx` 文件 → `POST /api/ext/games/{gameId}/flags/import/preview`
2. 系统解析列名,返回 9 列 + 5 行预览
3. Admin 配置列映射 (mapping) → 再次 preview 返回 14 repos / 20 submodules / 59 tracks
4. Admin 执行导入 → `POST /api/ext/games/{gameId}/flags/import` (mode=Upsert)
5. 系统创建 GameChallenges + ChallengeTracks + ChallengeFlags + ChallengeLevelConfigs

#### 3.3.2 列映射配置 (FlagImportMapping)

```json
{
  "RepositoryColumn": "所属仓库",
  "SubmoduleColumn": "子模块",
  "TrackFilePathColumn": "文件路径",
  "TrackCategoryColumn": "参考类别",
  "TrackReferenceUrlColumn": "社区公开PR",
  "MatchBy": "FilePath",
  "LevelColumns": [
    {
      "Level": 1,
      "ColumnName": "文件路径",
      "ExtraColumns": ["代码行数"],
      "AnswerType": "Exact",
      "Score": 5
    },
    {
      "Level": 2,
      "ColumnName": "commit id",
      "AnswerType": "Exact",
      "Score": 4,
      "BanOnAnswers": ["0"]
    },
    {
      "Level": 3,
      "ColumnName": "CWE(修复研判)",
      "AnswerType": "Exact",
      "Score": 1
    }
  ]
}
```

#### 3.3.3 R28 ban-trigger 数据驱动

数据集中 `hash(0)` 行 (arkweb_scheme_handler.cc) 的 commit id 列值是字面 `hash(0)`,平台导入时:
- L2 flag 值 = `hash(0)` (literal)
- L2 BanOnAnswers = `["0"]` — 玩家提交 `0` 触发**永久封禁**该级 (R27)

#### 3.3.4 导入边界 case 支持

| Case | 处理 |
|---|---|
| 单行格式 `c:1060` | L1 = `path:c:1060`, AnswerType=Exact |
| 范围格式 `c:550-559` | L1 = `path:c:550-559`, AnswerType=Exact |
| 多行格式 `c:281、c:286` | L1 = `path:c:281、c:286`, AnswerType=Exact |
| 组合 CWE `CWE-125; CWE-787` | L3 = `CWE-125; CWE-787`, AnswerType=Exact (顺序敏感) |
| `无法验证` L3 | L3 = `无法验证` (literal, 数据集语义问题) |
| `hash(0)` 行 | L2 = `hash(0)` + BanOnAnswers=`["0"]` |

### 3.4 防爆破与封禁机制 — R10/R11/R27

#### 3.4.1 三层状态模型

| 表 | 用途 | 触发 | 持续时间 |
|---|---|---|---|
| `ChallengeLockStates` | 临时锁定 | 错答超限 (SubmissionLimit) | `UnlocksAt` 时间自动解锁 |
| `ChallengeTrackLevelBanStates` | 永久封禁 (单级) | 提交 BanOnAnswers 中的答案 | 永久 |
| `ChallengeWrongCountStates` | 错答计数 | 每次错答 +1 | 比赛期间 |

#### 3.4.2 R10 临时锁定

- 每个题目可设 `SubmissionLimit` (默认 0=不限)
- 错答次数达 limit → 创建 `ChallengeLockState`
- `UnlocksAt = LockedAt + LockDurationMinutes`
- 锁定期间该队该题无法提交
- UI 显示倒计时和原因

#### 3.4.3 R27 永久封禁

- `ChallengeLevelConfig.BanOnAnswers` 配置 ban-trigger 答案 (e.g. `["0"]`)
- 玩家提交匹配 BanOnAnswers 的答案 → 创建 `ChallengeTrackLevelBanState`
- 该级 (Track + Level) 永久无法提交
- UI 显示红色 🔴 标记
- 玩家可继续提交其他级别 (e.g. L2 封禁后可跳到 L3)

#### 3.4.4 R11 三次冻结取消资格

> **当前状态**: 平台未实现累积 3 次冻结自动取消资格的全局机制。
> 临时锁定 (R10) 是单题维度,不跨题累积。
> 详见 [§10 已知限制](#10-已知限制与未实现项)。

### 3.5 多级 Flag 提交流程 — R5/R6/R8

#### 3.5.1 提交参数 (GameSubmitModel)

```typescript
{
  flag: string,        // 加密后的 flag
  level: number,       // 1, 2, or 3
  trackId?: string,    // L2+ 必填,L1 不传
}
```

#### 3.5.2 提交响应 (AnswerResult)

```typescript
{
  id: number,           // submission ID
  status: string,       // FlagSubmitted / Accepted / WrongAnswer
  message?: string,     // 附加消息 (e.g. "already been submitted")
  trackId?: string,     // L1 提交后返回,用于 L2+ 提交
  isTrackComplete?: boolean,  // L3 解完整个 track
  challengeLocked?: boolean,  // R10 锁定触发
  unlocksAt?: string,         // 解锁时间
  bannedTrackId?: string,     // R27 封禁触发
  bannedLevel?: number,       // 封禁的级别
  triggerAnswer?: string,     // 触发锁定/封禁的答案
}
```

#### 3.5.3 R5 部分完成得分

- L1 对 +5,L2 错 -2 (罚分),L3 不提交 → 总分 +3
- L1 对 +5,L2 对 +4,L3 错 -2 → 总分 +7
- 各 Level 独立计分,错误不回滚已得分

#### 3.5.4 Bug #4 修复: "+ New Vulnerability" 切换 Track

**问题**: 当玩家在 L2 of Track A 时,UI 只能提交 Track A 的 L2,无法切换到 Track B 的 L1。

**修复**: 新增 `startNewTrack` state + "+ New Vulnerability" 按钮:
- 点击后 `startNewTrack = true`,UI 显示 "✓ Starting new" + 绿色提示
- 下次提交时 `submitLevel = 1`, `submitTrackId = undefined`
- 后端将提交识别为 L1,匹配现有 Track 或创建新 Track (0day 工单审核后的)
- 提交成功后 `startNewTrack` 自动重置
- **UX 改进**: 按钮可 toggle (再次点击取消 "Starting new" 模式)

### 3.6 0day 工单审核 UI — R17/R18/R24

#### 3.6.1 Admin 审核弹窗

Admin 在 `/admin/games/{id}/zeroday-tickets` 点击 Approve/Reject 按钮弹出审核弹窗:

**Approve 弹窗字段**:
- 工单信息 (标题、提交者、描述)
- **Challenge 选择器** (仅当 `ticket.challengeId` 为 null 时显示) — Bug #6 修复
  - 提示文本: "This ticket has no ChallengeId. Select a challenge to award bonus to the team."
  - 下拉选择题目 (e.g. `ecmascript/builtins`)
  - 选中后 `reviewChallengeId` 传给后端,设置 `ticket.ChallengeId`
- Bonus Score (NumberInput,默认 0;R17 severity bonus 建议 60 for High)
- Comment (Textarea,可选)

**Reject 弹窗字段**:
- 工单信息
- Bonus Score (默认 0)
- Comment (Textarea,**必填**)

#### 3.6.2 Bug #5 修复: R24 错误消息清晰化

**问题**: Admin Approve T2 (R24 跨队重复) 时,后端抛出 `InvalidOperationException` 并返回完整堆栈 (developer exception page),前端通知显示一长串堆栈信息。

**修复**: 在 `utils/Shared.tsx` 的 `tryGetErrorMsg` 中新增 `tryExtractExceptionMessage`:
```typescript
const tryExtractExceptionMessage = (s: string): string | null => {
  if (typeof s !== 'string') return null
  const firstLine = s.split('\n')[0].trim()
  const colonIdx = firstLine.indexOf(':')
  return colonIdx >= 0 ? firstLine.slice(colonIdx + 1).trim() : firstLine
}
```

**效果**: 通知现在只显示 `R24: This 0day vulnerability has already been approved for another team. Subsequent submitters can only score as Nday, no 0day bonus.`,无堆栈。

#### 3.6.3 Bug #6 修复: Challenge 选择器

**问题**: 0day 工单无 ChallengeId 时 (玩家未关联题目),Admin Approve 时 bonus 无法加到队伍分数 (因为不知道加到哪个题)。

**修复**:
- 前端: Approve 弹窗新增 Challenge Select (仅当 `!ticket.challengeId` 时显示)
- 后端: `ZeroDayTicketReviewModel` 新增 `ChallengeId` 字段
- Repository: `ApproveTicket` 接受 `reviewChallengeId` 参数,若提供且工单无 ChallengeId,则验证并设置:
  ```csharp
  if (reviewChallengeId is int cid && ticket.ChallengeId is null)
  {
      var challenge = await Context.GameChallenges
          .AsNoTracking()
          .FirstOrDefaultAsync(c => c.Id == cid && c.GameId == ticket.GameId, token);
      if (challenge is null)
          throw new InvalidOperationException($"Challenge {cid} not found in game {ticket.GameId}");
      ticket.ChallengeId = cid;
  }
  ```

### 3.7 排行榜与得分计算

#### 3.7.1 得分组成

每队总分 = ∑ (题目得分) + ∑ (0day bonus)

- **题目得分**: 动态分值 (基于解题队伍数衰减) + 三血奖励
- **0day bonus**: R17 severity bonus (Critical=100, High=60, Medium=30, Low=10)

#### 3.7.2 多级 Flag 计分

- L1 = 5 分,L2 = 4 分,L3 = 1 分 (合计 10 分/track)
- 解完 L1+L2+L3 = "已解 1 个漏洞"
- 题目基础分 (e.g. 100) 按队伍解出的 track 数比例分配? 或解出任意 track 即得满分? — 由动态分值公式决定

#### 3.7.3 排行榜 UI

- `/games/{id}/scoreboard` 页面
- 显示: 排名、队伍、解题数、总分
- 按题目分组显示每队得分
- 支持导出 CSV

---

## 4. 数据模型变更

### 4.1 新增表

| 表 | 用途 |
|---|---|
| `ZeroDayTickets` | 0day 工单 |
| `ChallengeTracks` | 漏洞链 (一个题目多个 track) |
| `ChallengeLockStates` | 临时锁定状态 (R10) |
| `ChallengeTrackLevelBanStates` | 永久封禁状态 (R27) |
| `ChallengeWrongCountStates` | 错答计数 |
| `ChallengeLevelConfigs` | 每个 Level 的配置 (Score、BanOnAnswers、SubmissionLimit) |
| `ChallengePenalties` | 罚分记录 |

### 4.2 修改的表

| 表 | 变更 |
|---|---|
| `ChallengeFlags` | 新增 `Level`、`TrackId`、`AnswerType`、`RawAnswer`、`NormalizedAnswerKey`、`IsVisible` 字段 |
| `GameChallenges` | 新增 `LockDurationMinutes`、`Repository`、`Submodule` 等字段 |

### 4.3 EF Core 迁移

- `20260628110230_ExpandZeroDayTickets` — 扩展 ZeroDayTickets 表 (新增 8 列 + CreatedTrackId 索引)
- 早期迁移已包含 ChallengeTracks / ChallengeFlags 多级模型

---

## 5. API 端点变更

### 5.1 新增端点

#### 0day 工单 (`/api/ext/zeroday-tickets`)

| Method | Path | 用途 |
|---|---|---|
| POST | `/` | 创建 0day 工单 |
| GET | `/mine` | 获取我的工单 |
| GET | `/{ticketId}` | 获取工单详情 |
| GET | `/` | (Admin) 获取所有工单 |
| PUT | `/{ticketId}/approve` | (Admin) 审核通过 |
| PUT | `/{ticketId}/reject` | (Admin) 审核拒绝 |

#### Track 管理 (`/api/edit/games/{gameId}/challenges/{challengeId}/tracks`)

| Method | Path | 用途 |
|---|---|---|
| GET | `/` | 列出所有 track |
| POST | `/` | 创建 track |
| POST | `/full` | 创建完整 track (含 flags) |
| GET | `/{trackId}` | 获取 track 详情 |
| PUT | `/{trackId}` | 更新 track |
| DELETE | `/{trackId}` | 删除 track |

#### Level 配置 (`/api/edit/games/{gameId}/challenges/{challengeId}/levels`)

| Method | Path | 用途 |
|---|---|---|
| GET | `/` | 列出所有 level 配置 |
| POST | `/` | 创建 level 配置 |
| GET | `/{levelId}` | 获取 level 配置 |
| PUT | `/{levelId}` | 更新 level 配置 |
| DELETE | `/{levelId}` | 删除 level 配置 |

#### Flag 导入 (`/api/ext/games/{gameId}/flags/import`)

| Method | Path | 用途 |
|---|---|---|
| POST | `/preview` | 预览 Excel 文件 |
| POST | `/` | 执行导入 (mode=Upsert/Replace) |

### 5.2 修改的端点

#### Game 提交 (`/api/game/{gameId}/challenges/{challengeId}`)

提交 body 新增字段:
```json
{
  "flag": "...",
  "level": 1,
  "trackId": "optional-for-L2+"
}
```

响应新增字段:
```json
{
  "trackId": "...",
  "isTrackComplete": false,
  "challengeLocked": false,
  "unlocksAt": null,
  "bannedTrackId": null,
  "bannedLevel": null,
  "triggerAnswer": null
}
```

#### Game 获取题目详情 (`/api/game/{gameId}/challenges/{challengeId}`)

响应新增字段:
```json
{
  "tracks": [...],
  "currentLevel": 1,
  "activeTrackId": null,
  "challengeLock": {
    "isLocked": false,
    "unlocksAt": null,
    "reason": null,
    "bannedTrackId": null,
    "bannedLevel": null
  }
}
```

---

## 6. 前端 UI 变更

### 6.1 新增页面

#### 玩家端

- `/games/{id}/zeroday-tickets` — 0day 工单列表 + 提交弹窗
  - 工单列表: 显示状态、bonus、审核意见
  - 提交弹单: Title/Description/ChallengeId/Severity/Module/FilePath/LineNumber/CommitHash/CWE/PoC

#### 管理端

- `/admin/games/{id}/zeroday-tickets` — 0day 工单审核
  - 状态筛选 (All/Pending/Approved/Rejected)
  - 工单卡片: 标题、状态、提交者、Challenge、时间、描述、审核意见
  - Approve/Reject 弹窗

### 6.2 修改的组件

#### `ChallengeModal.tsx`

新增 `MultiLevelProgress` 子组件:
- 显示 track 选择器 + level 进度
- 显示锁定/封禁状态 (`ChallengeLockNotice`)
- 显示可见 flag 列表

#### `GameChallengeModal.tsx`

- 多级 flag 提交逻辑 (L1 不传 trackId,L2+ 必传)
- 缓存 `currentTrackId` 用于 L2+ 提交
- "+ New Vulnerability" 切换 track (Bug #4 修复)
- 处理锁定/封禁/重复提交的通知

#### `utils/Shared.tsx`

- `tryGetErrorMsg` 新增 `tryExtractExceptionMessage` (Bug #5 修复)

### 6.3 文件名规范化

- `ImportFlags.tsx` → `import-flags.tsx` (kebab-case)
- `ZeroDayTickets.tsx` → `zeroday-tickets.tsx` (kebab-case)

---

## 7. 核心业务流程

### 7.1 Nday 解题流程 (Phase 2/6)

```
玩家打开题目弹窗
  ↓
前端 GET /api/game/{gid}/challenges/{cid} → 返回 tracks + currentLevel
  ↓
玩家选择 track (或使用默认 activeTrackId)
  ↓
玩家输入 flag,点击 "提交 flag"
  ↓
前端 POST /api/game/{gid}/challenges/{cid} { flag, level, trackId? }
  ↓
后端 ScoringService.ProcessSubmission:
  1. 验证 flag (按 Level + TrackId 匹配 ChallengeFlag)
  2. 检查 R10 锁定状态 → 若锁定,返回 challengeLocked=true
  3. 检查 R27 封禁状态 → 若封禁,返回 bannedTrackId
  4. 检查 BanOnAnswers → 若匹配,创建 BanState,返回 banned
  5. 匹配正确 → 创建 Submission (Accepted),更新 GameScore
  6. 匹配错误 → 创建 Submission (WrongAnswer),更新 WrongCount
     - 若 WrongCount 达 limit → 创建 LockState
  7. 返回结果 (含 trackId for L1, isTrackComplete for L3)
  ↓
前端轮询 GET /api/game/{gid}/challenges/{cid}/status/{submissionId}
  ↓
状态变为 Accepted/WrongAnswer → 显示通知 + 更新 UI
```

### 7.2 0day 工单流程 (Phase 4/5)

```
玩家在 /games/{gid}/zeroday-tickets 点击 "提交 0day 工单"
  ↓
填写表单: Title/Description/ChallengeId?/Severity/Module/FilePath/LineNumber/CommitHash/CWE/PoC
  ↓
POST /api/ext/zeroday-tickets { ... }
  ↓
后端创建 ZeroDayTicket (Status=Pending)
  ↓
Admin 在 /admin/games/{gid}/zeroday-tickets 看到 Pending 工单
  ↓
Admin 点击 Approve:
  - 若 ticket.ChallengeId 为 null → 弹窗显示 Challenge 选择器
  - Admin 选择 Challenge,设置 BonusScore,点击 Approve
  ↓
PUT /api/ext/zeroday-tickets/{id}/approve { bonusScore, comment, challengeId? }
  ↓
后端 ZeroDayTicketRepository.ApproveTicket:
  1. 检查 R24 跨队重复 (同 FilePath+LineNumber 已有 Approved 工单) → 若重复,抛异常
  2. 若 reviewChallengeId 提供 && ticket.ChallengeId 为 null → 设置 ticket.ChallengeId
  3. 创建 ChallengeTrack (Category="0day", SortOrder=9999)
  4. 创建 3 个 ChallengeFlag (L1=FilePath:LineNumber, L2=CommitHash, L3=CWE)
  5. 设置 ticket.Status=Approved, ticket.CreatedTrackId=新 Track ID
  6. 若 ticket.ChallengeId 有值 → 加 bonus 到队伍分数 (GameScore)
  7. 创建 Submission (Status=Accepted) 记录 bonus
  ↓
玩家在工单列表看到 Approved + BonusScore
后续所有队伍可在题目弹窗看到新 track,可解 L1+L2+L3 得 10 分 (Nday)
```

### 7.3 R24 跨队重复判定流程 (Phase 3)

```
Team A 提交 0day 工单 T1 (FilePath=test/v.cpp, LineNumber=c:1)
  → T1 Pending
  ↓
Team B 提交 0day 工单 T2 (FilePath=test/v.cpp, LineNumber=c:1)
  → T2 Pending (无跨队检查在提交时,只在审核时)
  ↓
Admin Approve T1 → T1 Approved,创建 Track
  ↓
Admin Approve T2:
  - 后端检查: SELECT * FROM ZeroDayTickets WHERE FilePath='test/v.cpp' AND LineNumber='c:1' AND Status='Approved'
  - 找到 T1 → 抛 InvalidOperationException ("R24: ...")
  - 前端通知显示清晰错误消息 (Bug #5)
  - T2 保持 Pending
  ↓
Admin 显式 Reject T2 (Comment 必填) → T2 Status=Rejected
```

### 7.4 Excel 批量导入流程 (Phase 1)

```
Admin 在 /admin/games/{gid}/import-flags 上传 .xlsx
  ↓
POST /api/ext/games/{gid}/flags/import/preview (file)
  → 返回解析的列名 + 5 行预览
  ↓
Admin 配置 mapping (RepositoryColumn, SubmoduleColumn, LevelColumns, ...)
  ↓
POST /api/ext/games/{gid}/flags/import/preview (file + mapping)
  → 返回 14 repos / 20 submodules / 59 tracks 预览
  ↓
Admin 确认,POST /api/ext/games/{gid}/flags/import (file + mapping + mode=Upsert)
  ↓
后端 FlagImportService.ImportFlagsToGameAsync:
  1. 解析 xlsx,按 mapping 提取每行
  2. 按 (Repository, Submodule) 分组 → 创建/更新 GameChallenge
  3. 按 FilePath 创建 ChallengeTrack
  4. 按 LevelColumns 创建 3 个 ChallengeFlag (L1/L2/L3)
  5. 按 BanOnAnswers 配置 ChallengeLevelConfig
  6. 返回 Imported=59, CreatedChallenges=22, Failed=0
```

---

## 8. 规则符合性矩阵 (R1–R30)

| 规则 | 描述 | 实现状态 | 验证 Phase |
|---|---|---|---|
| **R1** | 0 人工全智能化挖洞,初赛+决赛 | N/A (背景) | — |
| **R2** | 0day 经评审组审核 | ✅ Admin = 评审组 | Phase 5 |
| **R3** | 0day 奖金由奖励计划提供 | N/A (资金流) | — |
| **R4** | Nday = 真实已修复漏洞 | ✅ Excel 导入数据集 | Phase 1 |
| **R5** | 满分 10 分,动态分值,部分完成 | ✅ L1/L2/L3 独立计分 | Phase 2.6 |
| **R6** | 4 级 flag 链 (file:line/commit/CWE/引入) | ✅ 3 级实现 (L4 未实现) | Phase 1/2 |
| **R7** | 4 级分值 5/4/1 | ✅ ChallengeLevelConfig | Phase 1.8 |
| **R8** | 精确到代码行标记 | ✅ L1 格式严格 (Exact/SingleLine/LineRange/MultiLine) | Phase 2.11 |
| **R9** | commit 语义 (引入 vs 修复) | ✅ 数据集 commit 列直接用作 L2 | Phase 1.10 |
| **R10** | 防爆破冻结当日 | ✅ ChallengeLockStates | Phase 2.12 |
| **R11** | 3 次冻结取消资格 | ❌ 未实现 (跨题累积) | — |
| **R12** | 晋级前 10 + ≥5 个 ≥9 分 Nday | N/A (外部赛事) | — |
| **R13** | 后 1/3 冲刺前 5 晋级 | N/A | — |
| **R14** | 晋级 15 支,XCTF 总决赛 | N/A | — |
| **R15** | 0day 提交入口与 Nday 分离 | ✅ /zeroday-tickets 独立路由 | Phase 4.1 |
| **R16** | 工单数量限制 | ⚠️ 未实现 (每队配额) | — |
| **R17** | 严重度加分 N×100/60/30/10 | ✅ Severity enum + BonusScore | Phase 4/5 |
| **R18** | 误报申诉转 0day | ✅ Admin Approve 时可升级 | Phase 5.2 |
| **R19** | 0day 加成需 Nday ≥200 | ❌ 未实现 (用户同意不修) | — |
| **R20** | 未在数据集/无效漏洞 0 分 | ✅ Admin Reject | Phase 5.3/5.4 |
| **R21** | 决赛 0day 须在指定仓库 | ⚠️ 未实现仓库白名单 | — |
| **R22** | 不在指定仓库不得分 | ⚠️ 同上 | — |
| **R23** | 总积分 = 0day×0.5 + 答辩×0.2 + 测评×0.3 | N/A (决赛外部) | — |
| **R24** | 同一漏洞仅首队得分 (0day);Nday 多队独立 | ✅ 0day 跨队 reject + Nday 多队独立 | Phase 3/6 |
| **R25** | F1 Score 指标 | ❌ 未实现 (用户同意不修) | — |
| **R26** | AI 工具使用 <50% 不积分 | N/A (评审主观) | — |
| **R27** | ban-trigger 永久封禁 | ✅ BanOnAnswers + BanState | Phase 2.3 |
| **R28** | hash(0) 数据源 | ✅ 导入时 BanOnAnswers=["0"] | Phase 1.10 |
| **R29** | 类别不符/可争议 误报申诉 | ✅ 工单系统支持 | Phase 4.5 |
| **R30** | 组合 CWE 顺序 | ✅ Exact 强制顺序 | Phase 2.9 |

### 8.1 已实现规则统计

- **完全实现**: R2, R4, R5, R6 (3级), R7, R8, R9, R10, R15, R17, R18, R20, R24, R27, R28, R29, R30 — **17 条**
- **未实现**: R11 (跨题累积冻结), R16 (工单配额), R19 (Nday≥200 gate), R21/R22 (仓库白名单), R25 (F1 Score) — **6 条**
- **N/A (外部赛事/资金/评审)**: R1, R3, R12, R13, R14, R23, R26 — **7 条**

---

## 9. 部署与配置

### 9.1 Docker Compose 配置

```yaml
# docker-compose.yml (关键部分)
services:
  gzctf:
    image: gzctf/gzctf:latest  # 或自构建
    environment:
      - ConnectionStrings__Database=Host=db;Port=5432;Database=gzctf;Username=gzctf;Password=${DB_PASSWORD:-gzctf_pass}
      - ADMIN_PASSWORD=${ADMIN_PASSWORD:-Admin@2022}
    ports:
      - "8081:8081"
    depends_on: [db, redis]
  db:
    image: postgres:17-alpine
    environment:
      - POSTGRES_USER=gzctf
      - POSTGRES_PASSWORD=${DB_PASSWORD:-gzctf_pass}
      - POSTGRES_DB=gzctf
  redis:
    image: redis:7-alpine
```

### 9.2 开发环境

```yaml
# docker-compose.override.yml
services:
  gzctf:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

### 9.3 前端构建

```bash
cd src/GZCTF/ClientApp
pnpm install
pnpm run build  # 输出到 build/
# 部署: docker cp build/. <gzctf-container>:/app/wwwroot/
```

### 9.4 数据库迁移

```bash
# 在容器内执行 EF Core 迁移
docker exec <gzctf-container> dotnet ef database update
# 或重启容器,平台自动应用迁移
```

---

## 10. 已知限制与未实现项

### 10.1 比赛规则未实现项

| 规则 | 描述 | 状态 | 备注 |
|---|---|---|---|
| **R11** | 3 次冻结取消资格 | ❌ 未实现 | 当前 R10 是单题维度锁定,无跨题累积机制 |
| **R16** | 每队工单配额 | ❌ 未实现 | 0day 工单无数量限制 |
| **R19** | 0day 加成需 Nday≥200 | ❌ 未实现 | 用户明确同意不修 |
| **R21/R22** | 决赛指定仓库白名单 | ❌ 未实现 | 0day 工单不校验 FilePath 是否在指定仓库 |
| **R25** | F1 Score 指标 | ❌ 未实现 | 用户明确同意不修 |
| **R6 L4** | 漏洞引入 commit (第 4 级) | ❌ 未实现 | 当前仅 3 级 (file:line/commit/CWE) |

### 10.2 已知 UX 改进点

- **0day 工单无 ChallengeId 时**: Admin 必须手动选择 Challenge 才能加 bonus (Bug #6 修复后的设计)
- **"+ New Vulnerability" 模式**: 已支持 toggle (Bug #4 UX 改进)
- **R24 错误消息**: 已清晰化 (Bug #5 修复)

### 10.3 性能与稳定性

- **Docker 桥接网络不稳定**: 在某些环境下,Docker 容器间通信会周期性中断 (Host is unreachable)。临时修复: `sudo systemctl restart docker`。根本原因待查。
- **Mantine Select combobox 自动化**: 合成 DOM 事件无法触发 Mantine Select 的选项选中,需使用 Playwright `press` 命令发送真实键盘事件。

### 10.4 数据集语义问题

- **`无法验证` L3**: 数据集中 5 行的 L3 是 `无法验证` 字面字符串,玩家若提交该字面值会得分。这是数据集维护者的语义问题,非平台 bug。
- **`hash(0)` 行**: 数据集中 commit id 列值是 `hash(0)` 而非 `0`,平台导入时把它当作 L2 flag 值。BanOnAnswers=`["0"]` 确保玩家提交 `0` 触发永久封禁 (R27/R28)。

---

## 附录 A: 关键文件清单

### 后端 (C#)

| 文件 | 用途 |
|---|---|
| `Controllers/ZeroDayTicketController.cs` | 0day 工单 API |
| `Controllers/TrackController.cs` | Track 管理 API |
| `Controllers/LevelConfigController.cs` | Level 配置 API |
| `Controllers/FlagImportController.cs` | Excel 导入 API |
| `Models/Data/ZeroDayTicket.cs` | 0day 工单模型 |
| `Models/Data/ChallengeTrack.cs` | 漏洞链模型 |
| `Models/Data/ChallengeFlag.cs` | Flag 模型 (含 Level/TrackId) |
| `Models/Data/ChallengeLockState.cs` | R10 临时锁定 |
| `Models/Data/ChallengeTrackLevelBanState.cs` | R27 永久封禁 |
| `Repositories/ZeroDayTicketRepository.cs` | 0day 工单仓储 (含 R18 闭环 + R24 检查) |
| `Services/Import/FlagImportService.cs` | Excel 导入服务 |
| `Services/Scoring/ScoringService.cs` | 计分服务 (多级 flag + 0day bonus) |
| `Migrations/20260628110230_ExpandZeroDayTickets.cs` | 最新迁移 |

### 前端 (React/TypeScript)

| 文件 | 用途 |
|---|---|
| `pages/games/[id]/zeroday-tickets.tsx` | 玩家 0day 工单页 |
| `pages/admin/games/[id]/zeroday-tickets.tsx` | Admin 0day 审核页 |
| `pages/admin/games/[id]/import-flags.tsx` | Admin Excel 导入页 |
| `components/ChallengeModal.tsx` | 题目弹窗 (含 MultiLevelProgress) |
| `components/GameChallengeModal.tsx` | 游戏题目弹窗 (含 Bug #4 修复) |
| `components/ChallengeLockNotice.tsx` | 锁定/封禁提示组件 |
| `utils/Shared.tsx` | `tryGetErrorMsg` (含 Bug #5 修复) |
| `Api.ts` | API 客户端 (含 0day 工单 + 多级 flag 类型) |

### 测试与文档

| 文件 | 用途 |
|---|---|
| `trace.md` | 完整 E2E 测试审计日志 (Phase 0–8) |
| `docs/test.md` | 测试计划 (R1–R30 映射) |
| `docs/OH_CTF_ADAPTATION.md` | 本文档 |
| `trace-shots/phase8/` | Phase 8 最终验证截图 (10 张) |

---

## 附录 B: Bug 修复历史 (Phase 8)

| Bug | 描述 | 修复方式 | 验证方式 |
|---|---|---|---|
| #1 | Migration 缺 .Designer.cs | 重新生成 `20260628110230_ExpandZeroDayTickets` + ModelSnapshot 更新 | DB 有 8 个新列 ✓ |
| #4 | UI 无法切换 track (currentLevel>1) | `startNewTrack` state + "+ New Vulnerability" 按钮 (toggle-able) | playerA 在 L2 of T1 时切换到 TRACK 1 L1 ✓ |
| #5 | R24 错误消息显示完整堆栈 | `tryExtractExceptionMessage` 提取首行 | 通知显示 "R24: ..." 无堆栈 ✓ |
| #6 | 0day 工单无 ChallengeId 时 bonus 丢失 | Admin 审核弹窗新增 Challenge 选择器 | T1 (无 ChallengeId) Approve 时选中 ecmascript/builtins ✓ |

---

**文档版本**: 1.0  
**最后更新**: 2026-06-28  
**维护者**: OpenHarmony CTF 平台适配团队
