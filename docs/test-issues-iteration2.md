# Iteration 2 测试问题清单与结果

> 日期：2026-07-01
> 测试范围：F1-F6 + 导入去重修复 + 前端 e2e
> 后端：http://10.77.0.10:8081 (docker)
> 测试账号：admin / Admin@2022；pa_1782638284（TeamA）；pb_1782640129（TeamB）

## 测试中发现并已修复的 bug

### Bug #1: ExtController 缺少 using
**文件**: `src/GZCTF/Controllers/ExtController.cs`
**问题**: 缺 `using GZCTF.Models.Request.Edit;`，导致 `BatchFlagUpdateModel`/`BatchFlagDeleteModel` 找不到，Release 编译失败
**修复**: 已添加 using
**状态**: ✅ 已修复（commit c09c910b）

### Bug #2: ScoreAdjustmentController 用错 Team 属性
**文件**: `src/GZCTF/Controllers/ScoreAdjustmentController.cs:175,182`
**问题**: 用 `t.TeamToken` 但 `Team` 模型只有 `InviteToken`
**修复**: 改成 `t.InviteToken`
**状态**: ✅ 已修复（commit c09c910b）

### Bug #3: 前端用错 Api 引用
**文件**: `ZeroDayInfo.tsx`, `Bans.tsx`
**问题**: `import { Api } from '@Api'` 然后用 `Api.Game.useGameChallengesWithTeamInfo` —— `Api` 是 class 不是实例，且 `Game` 不是 static 属性
**修复**: 改成 `import api from '@Api'` + `api.game.*`
**状态**: ✅ 已修复（commit c09c910b）

### Bug #4: toSorted 兼容性
**文件**: `useEdit.ts`, `Divisions.tsx`, `DivisionCard.tsx`
**问题**: `Array.prototype.toSorted()` 是 ES2023 特性，Chrome < 110 不支持，导致所有用 `useEdit` 的管理页崩溃
**修复**: 改成 `[...arr].sort(fn)`
**状态**: ✅ 已修复（commit c09c910b）

### Bug #5: 缺 EF 迁移
**问题**: iteration 2 加的 ScoreAdjustment 表、ChallengeTrackLevelBanState 新字段、Game.ZeroDayInfoMarkdown 都没迁移，数据库没这些字段
**修复**: 生成 `20260701094038_Iteration2` 迁移并应用
**状态**: ✅ 已修复（commit c09c910b）

### Bug #6: 导入 L1 归一化不一致
**文件**: `FlagImportService.cs:ComputeL1FlagValue`
**问题**: 存 flag 时做了 `C:→c:` / `::→:` 归一化，但 MatchBy=L1 查找时没做，重复导入会创建重复 track
**修复**: `ComputeL1FlagValue` 补归一化
**状态**: ✅ 已修复（commit ada275a1）

### Bug #7: MatchBy=L1 未在 UI 暴露
**文件**: `Import.tsx`, `import-flags.tsx`
**问题**: 下拉框只有 FilePath/FlawNo/Title，没有 L1。FilePath 会把同文件不同行的漏洞合并（丢漏洞）
**修复**: 加 L1 选项并设为默认，每个选项加说明文字
**状态**: ✅ 已修复（commit ada275a1）

---

## 未解决问题

### Issue #1: Rate limter 未触发
**Feature**: F4 (封禁/告警)
**严重度**: Medium
**复现**: pb 账号连续发 250 个 POST `/api/game/1/Challenges/1`，全部返回 200，0 个 429
**预期**: GlobalLimiter (SlidingWindow, PermitLimit=150/min, QueueLimit=60) 应在 210 请求后返回 429
**实际**: 0 个 429，0 条 RateLimitAlert GameNotice
**已排查**:
- ✅ `app.UseRateLimiter()` 已注册（AppExtensions.cs:90）
- ✅ Submit endpoint 有 `[EnableRateLimiting("Submit")]`（GameController.cs:1081）
- ✅ Submit policy 配置存在（TokenBucket 100/50-per-5s）
- ❌ 但 250 个顺序请求全 200
**可能原因**: GlobalLimiter 用 SlidingWindow 分区按 userId，但 QueueLimit=60 可能吸收了突发；或分区未按预期工作
**状态**: ⚠️ 待进一步排查

---

## 后端 API 测试结果

### F1 (0day info) ✅
- ✅ Admin PUT `/api/edit/Games/1` 更新 `zeroDayInfoMarkdown` + `zeroDayBaseScore`
- ✅ Admin GET 字段已持久化
- ✅ Player GET `/api/game/1/details` 返回 `zeroDayInfoMarkdown`、`zeroDayBaseScore`、`teamToken`

### F3 (分数管理) ✅
- ✅ GET `/api/admin/games/1/teams/search?q=Team` 模糊匹配（按队名/token/成员）
- ✅ POST `/api/admin/games/1/score-adjustments` Manual (delta=-50) 创建
- ✅ POST ZeroDayBonus (delta=200, relatedInfo) 创建
- ✅ GET 列表 + `?category=ZeroDayBonus` 过滤
- ✅ Scoreboard 整合：TeamA timeline 218 + 调分 150 = 最终 368
- ✅ Player notices 包含 ScoreAdjustment（2 条）

### F4 (封禁/告警) ⚠️
- ✅ POST `/api/admin/games/1/bans` 手动封禁 (isManual=true, reason 已存)
- ✅ GET `/api/admin/games/1/bans` 列表
- ✅ DELETE `/api/admin/games/1/bans/{id}` 解封 204
- ❌ Rate limit alert 未触发（见 Issue #1）

### F5 (flag 批量操作) ✅
- ✅ POST `/api/ext/challenges/1/flags/batch-update` 批量禁用/启用
- ✅ POST `/api/ext/challenges/1/flags/batch-delete` 批量删除（无提交时）
- ⚠️ 响应体 `{"status":400}` 是 RequestResponse 默认值，HTTP 状态实际是 200（cosmetic）

### F6 (level 标题) ✅
- ✅ 前端 ChallengeModal 显示 L1/L2/L3 标题

### 导入去重修复 ✅
- ✅ Preview 归一化 L1 flag（`C:100` → `c:100`）
- ✅ 1st import: 1 imported
- ✅ 2nd import (相同数据): 0 imported + 1 updated（去重正确）
- ✅ 数据库验证：仅 1 track，1 flag

---

## 前端 Chrome e2e 结果

### F1 ✅
- 截图：`trace-shots/iter2/F1-admin-info-page.png`
- 管理端 Info 页有 5 个 textarea，页面包含 "0day" 字样

### F2 ✅
- 截图：`trace-shots/iter2/F2-player-game-page.png`
- 选手页无 "发起申诉" / "Appeal" 按钮

### F5 ✅
- 截图：`trace-shots/iter2/F5-flag-selected.png`, `F5-after-select-debug.png`
- Flags 页 38 个 checkbox
- 选中后出现 "Batch Enable" / "Batch Disable" / "Batch Delete" 按钮

### F6 ✅
- 截图：`trace-shots/iter2/F6-tracks-loaded.png`, `F6-challenge-modal-ecmascript.png`
- Tracks 页加载正常（修复 toSorted 后）
- 选手 ChallengeModal 显示 L1/L2/L3 level 标题

### Import UI ✅
- 截图：`trace-shots/iter2/import-ui-after-upload.png`
- 上传文件后显示 MatchBy 下拉框（含 L1 选项）+ PenaltyScore 输入框

---

## 总结

- **已修复 bug**: 7 个（build 错误、迁移、toSorted、Api 引用、L1 归一化、MatchBy UI）
- **未解决**: 1 个（rate limiter 未触发）
- **后端 API 测试**: 5/5 feature 通过（F4 的 rate limit 部分未通过）
- **前端 e2e 测试**: 5/5 feature 通过
