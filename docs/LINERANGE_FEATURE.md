# OpenHarmony CTF 漏洞挖掘赛 - 行号范围匹配功能开发报告

## 概述

为适配第二届 OpenHarmony CTF（Capture The Flaw）智能漏洞挖掘赛初赛需求，在 GZCTF 平台新增了 **行号范围匹配（LineRange）** Flag 匹配模式。参赛队提交漏洞所在行号或行号范围，系统自动判定是否命中预设答案。

## 功能设计

### 核心逻辑

- 每个漏洞文件路径作为一个 Challenge（题目）
- 文件中每个漏洞行/范围作为一个 Flag，分值为 4 分
- 参赛队提交行号（如 `56`）或范围（如 `77-82`）
- 匹配规则：**提交范围 ⊆ 答案范围**（submitted_start >= flag_start AND submitted_end <= flag_end）
- 防爆破：使用现有 submissionLimit=5 限制每题提交次数

### 匹配示例

| 答案 | 提交 | 结果 |
|------|------|------|
| 77-82 | 78-80 | 命中 |
| 77-82 | 77 | 命中 |
| 77-82 | 50-100 | 不命中（超出范围） |
| 56 | 56 | 命中 |
| 56 | c:56 | 命中（兼容 c: 前缀） |

## 技术实现

### 后端改动

1. **`Utils/Enums.cs`** — 新增 `FlagMatchMode` 枚举（Exact=0, LineRange=1）
2. **`Models/Data/Challenge.cs`** — 添加 `FlagMatchMode` 属性
3. **`Models/Data/GameChallenge.cs`** — Update() 方法支持新字段
4. **`Services/Scoring/ScoringService.cs`** — 新增 `MatchLineRange()` 和 `TryParseLineRange()` 方法，ProcessSubmission 根据 FlagMatchMode 分支匹配
5. **`Models/Request/Edit/ChallengeUpdateModel.cs`** — 添加可选 FlagMatchMode 字段
6. **`Models/Request/Edit/ChallengeEditDetailModel.cs`** — 添加字段并映射
7. **`Models/Request/Game/ChallengeDetailModel.cs`** — 暴露给前端

### 数据库迁移

- `20260514162237_AddFlagMatchMode.cs` — 为 GameChallenges 和 ExerciseChallenges 表添加 FlagMatchMode 列（smallint, default 0）

### 前端改动

1. **`Api.ts`** — 添加 FlagMatchMode 枚举和相关接口字段
2. **`ChallengeModal.tsx`** — LineRange 模式下显示 "漏洞行号" 标签和行号格式提示
3. **`admin/.../Index.tsx`** — 管理员编辑页添加 FlagMatchMode 下拉选择器
4. **国际化** — zh-CN/en-US 的 admin.json 和 challenge.json 添加相关翻译

### 基础设施

- **docker-compose.yml** — 端口绑定改为 `127.0.0.1:8080`（安全要求）
- **Dockerfile** — pnpm 版本固定为 v9（兼容 lockfile v9）

## 测试结果

### 编译验证
- 后端 `dotnet build` — 通过（0 Error, 1 Warning）
- 前端 `tsc --noEmit` — 通过（0 Error）
- Docker 镜像构建 — 通过

### 接口测试（本地 127.0.0.1:8080）

| 测试项 | 接口 | 结果 |
|--------|------|------|
| 创建比赛 | POST /api/edit/games | 通过 |
| 创建 Challenge | POST /api/edit/games/1/challenges | 通过，返回 flagMatchMode: "Exact" |
| 更新为 LineRange 模式 | PUT /api/edit/games/1/challenges/1 | 通过，flagMatchMode: "LineRange" |
| 添加 Flag | POST /api/edit/games/1/challenges/1/flags | 通过 |
| 获取 Challenge 详情 | GET /api/game/1/challenges/1 | 通过，flagMatchMode 正确返回 |
| 精确行号命中 (56) | POST /api/game/1/challenges/1 | Accepted |
| 范围内命中 (78-80 ⊆ 77-82) | POST /api/game/1/challenges/1 | Accepted |
| 超出范围不命中 (50-100) | POST /api/game/1/challenges/1 | WrongAnswer |
| 无效格式 (abc) | POST /api/game/1/challenges/1 | WrongAnswer |
| c: 前缀兼容 (c:56) | POST /api/game/1/challenges/1 | 命中（已提交过返回 duplicate） |
| 提交限制 (第6次) | POST /api/game/1/challenges/1 | 400 Submission limit reached |
| 记分板 | GET /api/game/1/scoreboard | Score: 8（2个flag×4分） |

### 安全验证
- 服务仅监听 127.0.0.1，外部无法访问
- 提交限制正常工作，防止爆破
- 无效输入被正确拒绝

## 管理员使用指南

1. 创建比赛，设置时间窗口
2. 创建 Challenge：标题填文件路径，类型选 StaticAttachment
3. 编辑 Challenge：
   - Flag Match Mode 选择 "行号范围匹配"
   - 最大提交次数设为 5
   - 关闭 Blood Bonus
   - OriginalScore 设为所有 flag 分数之和
4. 添加 Flag：每个漏洞行号/范围作为一个 flag，Score=4
5. 启用 Challenge

## 扩展性

- **CWE 评分**：未来可添加额外 Flag（Score=2, FlagMatchMode=Exact, FlagValue="CWE-787"）
- **PoC 评分**：通过现有 Ticket 系统人工审核，管理员 Approve 时设置 bonusScore=4
- **新匹配模式**：FlagMatchMode 枚举可扩展（如 RegexMatch 等）

## 提交信息

- Commit: `5d88882c`
- Branch: `main`
- Author: ZephyrVictor
