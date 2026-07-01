# OpenHarmony CTF 平台第二轮迭代测试计划

> 日期：2026-07-01
> 范围：F1-F6 共 6 个 feature 的后端接口测试 + 前端 Chrome e2e 验证
> 测试原则：先后端后前端 → 每种情况完整验证 → 关键步骤截图+trace → 列 issue 修复 → 复测

---

## 1. 测试流程总览

每个 feature 按如下顺序测试：

```
1. 后端接口测试（curl）
   ↓ 全部通过
2. 前端 Chrome e2e 验证（Playwright drive.js）
   ↓ 发现问题
3. 列 issue 并提交（docs/test-issues-iteration2.md）
   ↓
4. 修复
   ↓
5. 复测（完整重跑该 feature 的测试用例）
```

### 1.1 环境准备

- **后端**：`dotnet build` + `dotnet ef database update` + `dotnet run`（或 docker compose up gzctf）
- **前端**：`cd src/GZCTF/ClientApp && pnpm dev`（开发模式热更新）
- **浏览器**：Chrome + Playwright `drive.js` helper
- **截图目录**：`trace-shots/iter2/F{N}/`
- **trace 目录**：`trace-shots/iter2/F{N}/trace/`
- **issue 文件**：`docs/test-issues-iteration2.md`

### 1.2 前置数据

- 管理员账号：admin / Admin123!（已有）
- 比赛 gameId：1（或新建一场比赛用于测试）
- 至少 2 个队伍：TeamA、TeamB（已加入比赛）
- 至少 1 个多级 flag 题目（含 3 个 level，已导入 flag）

---

## 2. 后端接口测试

### 2.1 F1: 0day 提交说明 tab

#### 2.1.1 管理员更新比赛 ZeroDayInfoMarkdown

```bash
# 获取 admin token
TOKEN=$(curl -s -X POST http://localhost:3000/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"Admin123!"}' | jq -r .data.token)

# 更新比赛的 0day 文案
curl -s -X PUT http://localhost:3000/api/edit/Games/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "title": "测试比赛",
    "summary": "",
    "content": "",
    "hidden": false,
    "acceptWithoutReview": true,
    "writeupRequired": false,
    "teamMemberCountLimit": 5,
    "containerCountLimit": 3,
    "practiceMode": true,
    "start": 1735660800,
    "end": 1735747200,
    "writeupDeadline": 1735833600,
    "writeupNote": "",
    "bloodBonus": 0,
    "zeroDayInfoMarkdown": "# 0day 提交说明\n\n战队：{{teamName}}\nToken：{{teamToken}}\n平台：{{bugbounty_url}}",
    "zeroDayBaseScore": 2
  }' | jq .
```

**预期**：返回 200，`zeroDayInfoMarkdown` 和 `zeroDayBaseScore` 已更新。

#### 2.1.2 选手获取比赛详情（含 0day 文案）

```bash
# 用选手 token 获取比赛详情
PLAYER_TOKEN=<player_token>
curl -s http://localhost:3000/api/game/1/details \
  -H "Authorization: Bearer $PLAYER_TOKEN" | jq '.data.zeroDayInfoMarkdown'
```

**预期**：返回的 `zeroDayInfoMarkdown` 包含 `{{teamName}}`、`{{teamToken}}` 占位符（前端替换）。

### 2.2 F3: 队伍分数管理

#### 2.2.1 搜索队伍

```bash
# 按队名搜索
curl -s "http://localhost:3000/api/admin/games/1/teams/search?q=Team" \
  -H "Authorization: Bearer $TOKEN" | jq .

# 按 token 搜索
curl -s "http://localhost:3000/api/admin/games/1/teams/search?q=tok_" \
  -H "Authorization: Bearer $TOKEN" | jq .

# 按成员搜索
curl -s "http://localhost:3000/api/admin/games/1/teams/search?q=player" \
  -H "Authorization: Bearer $TOKEN" | jq .
```

**预期**：返回 `[{teamId, teamName, teamToken, members}]`，模糊匹配。

#### 2.2.2 创建手动调分

```bash
curl -s -X POST http://localhost:3000/api/admin/games/1/score-adjustments \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "teamId": 1,
    "delta": -50,
    "reason": "违规扣分测试",
    "category": "Manual"
  }' | jq .
```

**预期**：返回 201，记录已创建，`delta=-50`。

#### 2.2.3 创建 0day 加分

```bash
curl -s -X POST http://localhost:3000/api/admin/games/1/score-adjustments \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "teamId": 1,
    "delta": 200,
    "reason": "0day 奖励 - CVE-2024-XXXX - Critical",
    "category": "ZeroDayBonus",
    "relatedInfo": "CVE-2024-XXXX; Severity: Critical"
  }' | jq .
```

**预期**：返回 201，`category=ZeroDayBonus`，`relatedInfo` 已存。

#### 2.2.4 查询调分记录

```bash
# 全部
curl -s "http://localhost:3000/api/admin/games/1/score-adjustments?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN" | jq .

# 只看 0day 加分
curl -s "http://localhost:3000/api/admin/games/1/score-adjustments?category=ZeroDayBonus" \
  -H "Authorization: Bearer $TOKEN" | jq .
```

**预期**：返回分页列表，`total` 正确，过滤生效。

#### 2.2.5 验证分数已生效

```bash
# 查看记分牌，确认调分已计入
curl -s http://localhost:3000/api/game/1/scoreboard | jq '.data.items[0]'
```

**预期**：对应队伍的 `score` 包含调分 delta。

#### 2.2.6 验证 GameNotice 已写入

```bash
# 管理员查看告警通知（含 ScoreAdjustment）
curl -s http://localhost:3000/api/edit/Games/1/AlertNotices \
  -H "Authorization: Bearer $TOKEN" | jq .

# 选手查看通知（应看到 ScoreAdjustment 通报，不含 RateLimitAlert）
curl -s http://localhost:3000/api/game/1/notices \
  -H "Authorization: Bearer $PLAYER_TOKEN" | jq '.data[] | .type'
```

**预期**：
- 选手通知里有 `ScoreAdjustment` 类型
- 选手通知里**没有** `RateLimitAlert` 类型

### 2.3 F4: 封禁/告警

#### 2.3.1 手动封禁

```bash
# 先获取 trackId（从题目详情）
TRACK_ID=$(curl -s http://localhost:3000/api/game/1/Challenges/1 \
  -H "Authorization: Bearer $PLAYER_TOKEN" | jq -r '.data.tracks[0].trackId')

curl -s -X POST http://localhost:3000/api/admin/games/1/bans \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{
    \"teamId\": 2,
    \"challengeId\": 1,
    \"trackId\": \"$TRACK_ID\",
    \"level\": 3,
    \"reason\": \"测试手动封禁\"
  }" | jq .
```

**预期**：返回 200，`isManual=true`，`reason` 已存。

#### 2.3.2 被封禁队伍提交被拒

```bash
# 用 TeamB（被手动封禁）的 token 提交
TEAM_B_TOKEN=<team_b_player_token>
curl -s -X POST http://localhost:3000/api/game/1/Challenges/1 \
  -H "Authorization: Bearer $TEAM_B_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"flag":"test_answer"}' | jq .
```

**预期**：返回 400 或 200 但 `status=WrongAnswer`，`message` 含"已被封禁"。

#### 2.3.3 列出封禁

```bash
curl -s http://localhost:3000/api/admin/games/1/bans \
  -H "Authorization: Bearer $TOKEN" | jq .
```

**预期**：返回封禁列表，含手动封（`isManual=true`）和 trap 自动封（`isManual=false`）。

#### 2.3.4 解封

```bash
BAN_ID=<上一步返回的 ban id>
curl -s -X DELETE http://localhost:3000/api/admin/games/1/bans/$BAN_ID \
  -H "Authorization: Bearer $TOKEN" -w "%{http_code}"
```

**预期**：返回 204。

#### 2.3.5 限流告警

```bash
# 快速连续提交 100+ 次触发限流
for i in $(seq 1 120); do
  curl -s -X POST http://localhost:3000/api/game/1/Challenges/1 \
    -H "Authorization: Bearer $PLAYER_TOKEN" \
    -H 'Content-Type: application/json' \
    -d '{"flag":"wrong_answer_'$i'"}' > /dev/null &
done
wait

# 管理员查看告警
curl -s http://localhost:3000/api/edit/Games/1/AlertNotices \
  -H "Authorization: Bearer $TOKEN" | jq '.[] | select(.type=="RateLimitAlert")'
```

**预期**：
- 429 响应出现
- AlertNotices 里至少 1 条 `RateLimitAlert`
- 5 分钟内同一队同一题只记 1 条（去重）

### 2.4 F5: Flag 批量操作

#### 2.4.1 批量禁用

```bash
# 获取题目所有 flag ID
FLAG_IDS=$(curl -s http://localhost:3000/api/ext/challenges/1/flags \
  -H "Authorization: Bearer $TOKEN" | jq -r '.data[].id' | jq -R . | jq -s .)

curl -s -X POST http://localhost:3000/api/ext/challenges/1/flags/batch-update \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"flagIds\": $FLAG_IDS, \"isEnabled\": false}" | jq .
```

**预期**：返回 200，所有 flag `isEnabled=false`。

#### 2.4.2 批量启用

```bash
curl -s -X POST http://localhost:3000/api/ext/challenges/1/flags/batch-update \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"flagIds\": $FLAG_IDS, \"isEnabled\": true}" | jq .
```

#### 2.4.3 批量删除（无提交记录时）

```bash
# 用未提交过的 flag ID
curl -s -X POST http://localhost:3000/api/ext/challenges/1/flags/batch-delete \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"flagIds\": $FLAG_IDS}" | jq .
```

**预期**：返回 200，flag 已删除。

### 2.5 F6: Level 名字显示

#### 2.5.1 验证导入后 ChallengeLevelConfig.Title 已填

```bash
# 导入 flag 后，查看题目详情
curl -s http://localhost:3000/api/game/1/Challenges/1 \
  -H "Authorization: Bearer $PLAYER_TOKEN" | jq '.data.tracks[0].levels[].title'
```

**预期**：返回 `["文件路径", "commit", "CWE"]`（或导入时设置的列名）。

---

## 3. 前端 Chrome e2e 测试

> 使用 `drive.js` Playwright helper 驱动真实 Chrome。每个场景截图 + trace。

### 3.1 F1: 0day 提交说明 tab

#### 场景 3.1.1: 选手查看 0day tab

1. 选手登录 → 进入比赛 `/games/1`
2. 点击 WithGameTab 的 "0day 提交" tab
3. **截图**：`trace-shots/iter2/F1/01-zeroday-tab.png`
4. **验证**：
   - 页面渲染 markdown 文案
   - `{{teamName}}` 已替换为实际队名
   - `{{teamToken}}` 已替换为实际 token
   - bugbounty 链接可点击
5. 点击"前往 Bugbounty 平台" → 新窗口打开 `https://bugbounty.openharmony.cn/bugbounty/leak`

#### 场景 3.1.2: 管理员编辑 0day 文案

1. 管理员登录 → 比赛编辑 → Info tab
2. 滚动到 "0day 提交说明（Markdown）" 文本框
3. **截图**：`trace-shots/iter2/F1/02-admin-edit.png`
4. 修改文案 → 保存
5. 切回选手视角 → 0day tab → 验证文案已更新

#### 场景 3.1.3: 文案为空时

1. 管理员清空 ZeroDayInfoMarkdown → 保存
2. 选手刷新 0day tab
3. **验证**：显示黄色 Alert "管理员未配置 0day 提交说明"

### 3.2 F2: 申诉 + 0day 前端入口关闭

#### 场景 3.2.1: 申诉按钮已隐藏

1. 选手登录 → 提交记录页 `/games/1/submissions`
2. 找到一条 penalty 提交记录
3. **验证**：操作列没有"发起申诉"按钮
4. **截图**：`trace-shots/iter2/F2/01-no-appeal.png`

#### 场景 3.2.2: 比赛前置页 0day 按钮已隐藏

1. 选手进入比赛前置页 `/games/1`
2. **验证**：没有"0day Tickets"按钮
3. **截图**：`trace-shots/iter2/F2/02-no-zeroday-button.png`

#### 场景 3.2.3: 管理端 0day 审批 tab 已隐藏

1. 管理员进入比赛编辑页 `/admin/games/1`
2. 查看顶部 tab 栏
3. **验证**：没有"0day Tickets" tab
4. **截图**：`trace-shots/iter2/F2/03-no-admin-zeroday-tab.png`

### 3.3 F3: 队伍分数管理

#### 场景 3.3.1: 搜索队伍

1. 管理员进入比赛编辑 → "分数管理" tab
2. **截图**：`trace-shots/iter2/F3/01-score-tab.png`
3. 在搜索框输入 "Team" → 点搜索
4. **验证**：表格显示匹配队伍
5. **截图**：`trace-shots/iter2/F3/02-search-results.png`

#### 场景 3.3.2: 手动调分

1. 点队伍行"调整分数"按钮
2. **截图**：`trace-shots/iter2/F3/03-adjust-modal.png`
3. 填 delta=-30, reason="测试扣分"
4. 提交 → 验证成功提示
5. 查看记分牌 → 该队分数减少 30

#### 场景 3.3.3: 0day 加分

1. 点队伍行"0day 加分"按钮
2. **截图**：`trace-shots/iter2/F3/04-zeroday-bonus-modal.png`
3. 选 severity=Critical → 验证 delta 自动=100
4. 填 CVE 编号 + 理由 → 提交
5. 查看记分牌 → 该队分数增加 100

#### 场景 3.3.4: 调分记录

1. 进入"调分记录" tab
2. **截图**：`trace-shots/iter2/F3/05-ledger.png`
3. 切换类别过滤（全部 / 手动 / 0day）
4. **验证**：列表正确过滤

#### 场景 3.3.5: 0day 加分记录

1. 进入"0day 加分记录" tab
2. **截图**：`trace-shots/iter2/F3/06-zeroday-ledger.png`
3. **验证**：顶部统计卡片显示累计加分 + 各级别数量

#### 场景 3.3.6: 通知通报

1. 选手视角查看比赛通知
2. **验证**：通知流里有调分通报（如"队伍 TeamA 因 违规扣分测试 -30 分"）
3. **截图**：`trace-shots/iter2/F3/07-notice.png`

### 3.4 F4: 封禁/告警

#### 场景 3.4.1: 封禁管理 tab

1. 管理员进入"封禁管理" tab
2. **截图**：`trace-shots/iter2/F4/01-bans-tab.png`
3. **验证**：上方"限流告警"区，下方"封禁列表"区

#### 场景 3.4.2: 手动封禁

1. 点"手动封禁"按钮
2. **截图**：`trace-shots/iter2/F4/02-manual-ban-modal.png`
3. 搜索队伍 → 选 Challenge → 填 Track ID + Level + 理由 → 提交
4. 验证封禁列表新增一条 `isManual=true` 记录

#### 场景 3.4.3: 被封禁选手提交被拒

1. 切到被手动封禁的选手账号
2. 进入题目提交 → 提交该 (Track, Level) 的 flag
3. **验证**：返回"已被封禁，请联系管理员"
4. **截图**：`trace-shots/iter2/F4/03-banned-submit.png`

#### 场景 3.4.4: 解封

1. 管理员回到封禁管理 → 找到手动封记录 → 点"解封"
2. 选手重新提交该 (Track, Level) → 验证可提交
3. **截图**：`trace-shots/iter2/F4/04-after-unban.png`

#### 场景 3.4.5: 限流告警展示

1. 触发限流（快速提交 100+ 次）
2. 管理员刷新封禁管理 tab → 查看"限流告警"区
3. **验证**：告警列表有新记录
4. **截图**：`trace-shots/iter2/F4/05-rate-limit-alert.png`

#### 场景 3.4.6: trap 自动封记录可见

1. 让选手触发 trap 答案（提交 hash(0)）
2. 管理员封禁管理 → 验证列表新增 `isManual=false` 的 trap 自动封记录
3. 点解封 → 验证可解封 trap 误触

### 3.5 F5: Flag 批量操作

#### 场景 3.5.1: 多选 + 批量启停

1. 管理员进入题目编辑 → Flags tab
2. **截图**：`trace-shots/iter2/F5/01-flag-panel.png`
3. 勾选 3 个 flag 的 checkbox
4. **验证**：顶部浮现批量操作栏，显示"已选 3 项"
5. **截图**：`trace-shots/iter2/F5/02-batch-bar.png`
6. 点"批量禁用" → 验证 flag 状态变为 disabled
7. 点"批量启用" → 验证恢复

#### 场景 3.5.2: 批量删除

1. 勾选 2 个无提交记录的 flag
2. 点"批量删除" → 确认对话框 → 确认
3. **验证**：flag 已删除
4. **截图**：`trace-shots/iter2/F5/03-after-delete.png`

#### 场景 3.5.3: 全选 + 清除

1. 点批量操作栏的"全选" checkbox → 验证所有 flag 被选中
2. 点清除按钮 → 验证选择清空

#### 场景 3.5.4: 有提交记录的 flag 删除被拒

1. 勾选已有提交记录的 flag
2. 点批量删除
3. **验证**：返回 409 Conflict，flag 未删除

### 3.6 F6: Level 名字显示

#### 场景 3.6.1: 提交界面显示 level 名字

1. 选手进入题目详情（已导入 flag 的题目）
2. **截图**：`trace-shots/iter2/F6/01-level-titles.png`
3. **验证**：
   - 进度条显示 `L1 - 文件路径`、`L2 - commit`、`L3 - CWE`（或导入时设置的列名）
   - 当前 level badge 也显示名字

#### 场景 3.6.2: 未导入 level 名字回退

1. 找一个没设置 Title 的题目（手动创建的）
2. **验证**：进度条显示 `L1`、`L2`、`L3`（无后缀）

---

## 4. Issue 跟踪

### 4.1 Issue 文件格式

在 `docs/test-issues-iteration2.md` 记录每个问题：

```markdown
## Issue #N: [标题]
- **Feature**: F{1-6}
- **严重程度**: Critical / Major / Minor
- **场景**: 场景 3.x.x
- **复现步骤**:
  1. ...
  2. ...
- **预期**: ...
- **实际**: ...
- **截图**: trace-shots/iter2/F{N}/issue-{N}.png
- **状态**: Open / Fixed / Retested-Pass / Retested-Fail
- **修复 commit**: <hash>
```

### 4.2 修复 + 复测流程

1. 发现问题 → 记录 issue
2. 修复代码 → commit（message 含 `fix(iter2/F{N}): issue #N ...`）
3. 复测：完整重跑该 feature 的所有测试用例（不只是出问题的那个）
4. 复测通过 → 更新 issue 状态为 `Retested-Pass`
5. 复测失败 → 重新修复，重复 2-4

---

## 5. 测试检查清单

### 5.1 后端接口（curl）

- [ ] F1: GET /api/game/{id}/details 返回 zeroDayInfoMarkdown
- [ ] F1: PUT /api/edit/Games/{id} 可更新 zeroDayInfoMarkdown
- [ ] F3: GET /api/admin/games/{id}/teams/search?q= 模糊搜索生效
- [ ] F3: POST /api/admin/games/{id}/score-adjustments 创建手动调分
- [ ] F3: POST /api/admin/games/{id}/score-adjustments 创建 0day 加分
- [ ] F3: GET /api/admin/games/{id}/score-adjustments 分页 + 过滤
- [ ] F3: 调分后记分牌分数已更新
- [ ] F3: 调分后 GameNotice 已写入（ScoreAdjustment 类型）
- [ ] F4: POST /api/admin/games/{id}/bans 手动封禁
- [ ] F4: 被封禁队伍提交被拒（返回"已被封禁"）
- [ ] F4: GET /api/admin/games/{id}/bans 列表含手动+自动封
- [ ] F4: DELETE /api/admin/games/{id}/bans/{id} 解封
- [ ] F4: 限流触发后 AlertNotices 有 RateLimitAlert 记录
- [ ] F4: 选手通知不含 RateLimitAlert（admin-only）
- [ ] F4: 5 分钟去重生效
- [ ] F5: POST /api/ext/challenges/{id}/flags/batch-update 启用/禁用
- [ ] F5: POST /api/ext/challenges/{id}/flags/batch-delete 删除
- [ ] F5: 有提交记录的 flag 删除返回 409
- [ ] F6: 导入后 ChallengeLevelConfig.Title = ColumnName（无 LevelTitle 时）

### 5.2 前端 Chrome e2e

- [ ] F1 场景 3.1.1: 选手查看 0day tab，占位符替换正确
- [ ] F1 场景 3.1.2: 管理员编辑 0day 文案
- [ ] F1 场景 3.1.3: 文案为空时显示 Alert
- [ ] F2 场景 3.2.1: 申诉按钮已隐藏
- [ ] F2 场景 3.2.2: 比赛前置页 0day 按钮已隐藏
- [ ] F2 场景 3.2.3: 管理端 0day 审批 tab 已隐藏
- [ ] F3 场景 3.3.1: 搜索队伍（队名/成员/token）
- [ ] F3 场景 3.3.2: 手动调分 → 记分牌更新
- [ ] F3 场景 3.3.3: 0day 加分（severity→delta 自动算）
- [ ] F3 场景 3.3.4: 调分记录账本 + 过滤
- [ ] F3 场景 3.3.5: 0day 加分记录页 + 统计卡片
- [ ] F3 场景 3.3.6: 选手通知流可见调分通报
- [ ] F4 场景 3.4.1: 封禁管理 tab（告警 + 封禁两区）
- [ ] F4 场景 3.4.2: 手动封禁弹窗
- [ ] F4 场景 3.4.3: 被封禁选手提交被拒
- [ ] F4 场景 3.4.4: 解封后可提交
- [ ] F4 场景 3.4.5: 限流告警展示
- [ ] F4 场景 3.4.6: trap 自动封记录可见 + 可解封
- [ ] F5 场景 3.5.1: 多选 + 批量启停
- [ ] F5 场景 3.5.2: 批量删除
- [ ] F5 场景 3.5.3: 全选 + 清除
- [ ] F5 场景 3.5.4: 有提交记录的 flag 删除被拒
- [ ] F6 场景 3.6.1: 提交界面显示 level 名字
- [ ] F6 场景 3.6.2: 未设置 Title 回退到 L{n}

### 5.3 i18n 验证

- [ ] 切换到 en-US → 所有新增文案显示英文
- [ ] 切换到 zh-CN → 所有新增文案显示中文
- [ ] 无硬编码中文/英文字符串（抽查 3 个组件）

---

## 6. 不在本次测试范围

- 0day 工单后端接口（前端已关闭，但后端代码保留，不测试）
- 申诉后端接口（同上）
- 其他 9 个 locale 的翻译（只测 zh-CN + en-US）
- bugbounty 平台对接（只验证链接跳转）
- 0day 加分自动化（管理员手动触发）
