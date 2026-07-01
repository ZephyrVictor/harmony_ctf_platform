# Iteration 2 测试问题清单

> 日期：2026-07-01
> 测试范围：F1-F6 + 导入去重修复

## 已发现问题

### Issue #1: Rate limiter 未触发

**Feature**: F4 (封禁/告警)
**严重度**: Medium
**复现**: 用 pb 账号连续发 250 个 POST `/api/game/1/Challenges/1`，全部返回 200，0 个 429
**预期**: GlobalLimiter (SlidingWindow, PermitLimit=150/min, QueueLimit=60) 应在 210 请求后返回 429；Submit policy (TokenBucket, 100 tokens, 50/5s) 也应限制
**实际**: 0 个 429，0 条 RateLimitAlert GameNotice
**可能原因**:
- `app.UseRateLimiter()` 调用顺序问题（在 auth 之前？）
- SlidingWindow 分区未生效
- 或限流配置被其他 middleware 覆盖
**状态**: 待修复

---

## 已验证通过

### F1 (0day info)
- ✅ Admin PUT `/api/edit/Games/1` 更新 `zeroDayInfoMarkdown` + `zeroDayBaseScore` 成功
- ✅ Admin GET `/api/edit/Games/1` 字段已持久化
- ✅ Player GET `/api/game/1/details` 返回 `zeroDayInfoMarkdown`、`zeroDayBaseScore`、`teamToken`

### F3 (分数管理)
- ✅ GET `/api/admin/games/1/teams/search?q=Team` 返回模糊匹配结果
- ✅ POST `/api/admin/games/1/score-adjustments` 创建 Manual (delta=-50) 成功
- ✅ POST 创建 ZeroDayBonus (delta=200, relatedInfo) 成功
- ✅ GET 列表 + category=ZeroDayBonus 过滤生效
- ✅ Scoreboard 整合：TeamA timeline 218 + 调分 150 = 最终 368
- ✅ Player notices 包含 ScoreAdjustment (2 条)

### F4 (封禁/告警)
- ✅ POST `/api/admin/games/1/bans` 手动封禁创建成功 (isManual=true, reason 已存)
- ✅ GET `/api/admin/games/1/bans` 列表正确
- ✅ DELETE `/api/admin/games/1/bans/{id}` 解封返回 204
- ❌ Rate limit alert 未触发 (见 Issue #1)
