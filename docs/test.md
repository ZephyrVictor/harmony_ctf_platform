# GZCTF OpenHarmony CTF 平台 — 完整 E2E 测试计划

> 本文档是 **测试计划** (test plan),与 `~/.claude/skills/gzctf-gameflow-test/SKILL.md` 对齐。
> SKILL.md 描述 driver 与 selectors,本文档描述"测什么、怎么验"。
> 执行时由 agent 驱动 Chrome (Playwright daemon),不写测试脚本。
>
> **设计原则**: 比赛规则逐条编号 (R1–R30),每条规则都映射到至少一个测试步骤。
> 规则核验矩阵 (§4) 列出每条规则的覆盖 Phase、验证方式、期望结果。

---

## 0. 测试目标

用真实 Chrome 浏览器,**从管理员 + 两个比赛选手**的视角,完整跑通
GZCTF OpenHarmony CTF 平台的比赛流程,覆盖:

- 数据集全量导入 (59 行, 14 仓库, 22 个挑战)
- 多级 flag 链 (file:line → commit → CWE, 5/4/1 分)
- ban-trigger 永久封禁
- 防爆破锁定 + 3 次取消资格
- 跨队重复不得分
- 0day 工单 4 种严重度加分
- 误报申诉转 0day
- 工单数量限制 + 工单内容越界判定
- 数据集外 Nday 不得分
- **0day 审核通过 → 进入数据集 → 后续选手作为 Nday 得分** (闭环)
- 多队排行榜 + 决赛 F1 Score 指标 (若平台实现)
- 比赛时间窗口边界 + 公开/私有切换

---

## 1. 比赛规则条目化 (R1–R30)

> 来源: `memo/比赛规则-第二届OpenHarmony CTF（Capture The Flaw）智能漏洞挖掘赛.docx`
> 补充: `memo/round1` (ban-trigger 提交 `0` 永久封禁该级)

### 1.1 比赛形式 (R1–R4)

| 编号 | 规则原文要点 | 测试 Phase |
|---|---|---|
| R1 | "0 人工"全智能化挖洞,分线上初赛 + 现场决赛 | N/A (平台无法验证"0 人工", 仅作为背景) |
| R2 | 初赛/决赛的 0day 均需通过 OpenHarmony 安全漏洞奖励计划提交, 判定由评审组决定 | Phase 4 (0day 工单入口) + Phase 5 (admin 审核 = 评审组角色) |
| R3 | 有效 0day 奖金由奖励计划提供 | N/A (资金流, 平台不实现) |
| R4 | Nday = 真实在 OpenHarmony Release 版本发现并已修复的漏洞 | Phase 1 (导入已修复漏洞数据集) |

### 1.2 初赛积分规则 (R5–R11)

| 编号 | 规则原文要点 | 测试 Phase |
|---|---|---|
| R5 | 每个有效发现满分 10 分, **动态分值, 根据完成度获得对应积分** | Phase 2.6 (部分完成: L1对+L2错 应得 5 而非 0) |
| R6 | 4 级 flag 链: 文件路径+触发代码行 / Commit hash / CWE 类型 / (漏洞引入?) | Phase 1.8 + Phase 2.2-2.7 |
| R7 | 4 级分值: 5 / 4 / 1 (合计 10) | Phase 1.8 (admin Levels 页) + Phase 2 加分验证 |
| R8 | **"精确到代码行标记"** — L1 答案格式严格性 | Phase 2.11 (L1 格式边界: 缺 `c:`、缺行号、多余空格) |
| R9 | "漏洞引入 commit" — 数据集 commit id 列的语义 (引入 vs 修复) | Phase 1.10 注明 + Phase 2 提交时使用数据集原值 |
| R10 | 防爆破: 禁止爆破猜想, 违规**冻结当日提交资格** | Phase 2.10 (错答超限 → 锁定 unlocksAt) |
| R11 | 冻结次数达 **3 次取消参赛资格** | Phase 2.10.扩展 (若平台实现, 触发 3 次冻结验证账号被禁) |

### 1.3 初赛晋级规则 (R12–R14)

| 编号 | 规则原文要点 | 测试 Phase |
|---|---|---|
| R12 | 赛程 2/3: 排名前 10 (或前 10%) 且**含 ≥5 个得分 ≥9 的 Nday** | N/A (晋级逻辑平台不实现, 仅排行榜展示) |
| R13 | 赛程后 1/3 排位冲刺: 抢先达 50 分前 5 晋级 | N/A (同上) |
| R14 | 晋级名额暂定 15 支, 冠军获 XCTF 总决赛入场券 | N/A (外部赛事, 平台不实现) |

### 1.4 0day 附加分规则 (R15–R20)

| 编号 | 规则原文要点 | 测试 Phase |
|---|---|---|
| R15 | 初赛期间挖出 0day, **通过 0day 漏洞提交入口** (与 Nday flag 入口分离) 提交 | Phase 4.1 (确认 `/games/$GAME_ID/zeroday-tickets` 与 flag 提交是不同入口) |
| R16 | **限定的审核工单数量限制内** — 每队工单配额 | Phase 4.6 (超量被拒) |
| R17 | 严重 N×100 / 高危 N×60 / 中危 N×30 / 低危 N×10, N=对应数量 | Phase 4.1-4.4 (4 严重度各一单) + Phase 5.1 (accept 后加分) |
| R18 | **针对模块漏洞被评估为 Flag 错误的误报进行申诉为 0day**, 审核通过可获 0day 积分 | Phase 4.5 (申诉) + Phase 5.2 (accept+升级) |
| R19 | 初赛阶段获得 0day 附加分的团队 **Nday 得分不得少于 200 分** | Phase 7 (排行榜验证: TeamA Nday < 200 时, 0day 加成是否被扣减) |
| R20 | 0day 提交但被审核为**未在预期数据集中的 Nday 或无效漏洞, 不获得积分** | Phase 5.3 (admin 把一个 0day 工单判定为"实为数据集已有 Nday" → 0 分) + Phase 5.4 (判为"无效漏洞" → 0 分) |

### 1.5 决赛规则 (R21–R26)

| 编号 | 规则原文要点 | 测试 Phase |
|---|---|---|
| R21 | 第一阶段挖掘 0day, 在指定代码仓范围 | Phase 4.7 (0day 工单提交**不在指定仓库**的文件 → 应被拒) |
| R22 | 不在指定代码仓范围内的漏洞不会获得比赛积分 | Phase 4.7 + Phase 5.5 (admin 拒) |
| R23 | 总积分 = 0day × 0.5 + 答辩 × 0.2 + 现场测评 × 0.3 | N/A (决赛外部环节, 平台可能不实现, 注明) |
| R24 | 同一漏洞仅第一个提交的团队获得积分, 后续记为重复不得分 | Phase 3.2 + 3.4 (跨队双向) + Phase 6.3 (0day→Nday 后原提交者不得分) |
| R25 | 现场测评: 漏洞定位 F1 Score + 漏洞验证 F1 Score | N/A (评测在平台外, 注明平台是否实现 F1 指标展示) |
| R26 | 报告 AI 工具使用 < 50%, 虽有效漏洞但不获积分 | N/A (评审主观判断, 平台不实现) |

### 1.6 平台补充规则 (R27–R30, 来自 memo/round1 + 数据集)

| 编号 | 规则来源 | 要点 | 测试 Phase |
|---|---|---|---|
| R27 | memo/round1 | 提交 `0` 错误后, 这个问题的 commit (这一级) **后续提交也不得分** (永久封禁) | Phase 2.3 + 2.4 (ban-trigger + post-ban 验证) |
| R28 | 数据集 sheet2 | `hash(0)` 行的 commit = `0` — 这是 ban-trigger 数据驱动源 | Phase 1.10 (DB 查 IsBanTrigger) |
| R29 | 数据集 sheet2 | 4 行"类别不符" + 12 行"可争议" — 误报申诉候选 | Phase 4.5 (选一个"类别不符" track) |
| R30 | 数据集 | 组合 CWE 如 `CWE-125; CWE-787` — 多答案 case | Phase 2.6 (L3 顺序敏感性: `CWE-787; CWE-125` 是否也接受) |

---

## 2. 数据集细节

### 2.1 文件

- 路径: `memo/OH社区第二届CTF大赛AI数据集（0608）修复.xlsx`
- Sheet 1 "6.0数据集": 59 行 × 9 列 (所属仓库 / 子模块 / 文件路径 / 代码行数 / commit id / 社区公开PR / 参考类别 / CWE(原标) / CWE(修复研判))
- Sheet 2 "终检核对": 59 行 × 7 列 (Case / 所属仓库 / 文件 / commit状态 / 类别吻合 / PR状态 / 终检备注)
- 14 个独立仓库, 22 个 (仓库, 子模块) 对 → 22 个 GameChallenge

### 2.2 L1 答案 (代码行数列) 3 种格式

| 格式 | 样例 | 行数 | L1 答案形态 |
|---|---|---|---|
| 单行 | `c:1060` | 41 | `<file>:c:1060` |
| 范围 | `c:550-559` | 11 | `<file>:c:550-559` |
| 多行 | `c:281、c:286` | 7 | `<file>:c:281、c:286` |

### 2.3 L2 (commit id) 边界

- 1 行 `hash(0)` → commit 值为 `0` (ban-trigger 源, R27/R28)
- 4 行 `无法验证` → commit 不可用,导入后该 track L2 应为空或特殊值
- 1 行 `候选·待确认(红)` + 1 行 `补齐·疑问` + 1 行 `已修正·待确认(红)` → 边界 case, 验证导入不崩

### 2.4 L3 (CWE 修复研判) 边界

- 5 行 `无法验证` → L3 应为空或特殊值, 提交任何 CWE 都应错
- 4 行组合 CWE 如 `CWE-125; CWE-787` → 多答案 case (R30)
- 单 CWE 主流: CWE-787(11) / CWE-476(10) / CWE-125(7) / CWE-362(6) / CWE-20(3) / CWE-674(2) / CWE-843(2) / CWE-22(2)

### 2.5 类别吻合 (误报申诉候选, R29)

- 4 行 `不符` — 原标与修复研判完全不同, 误报申诉首选
- 12 行 `可争议` — 原标与修复研判有争议
- 5 行 `无法验证` — 不可用

### 2.6 PR 状态边界 (导入健壮性)

- 2 行 `不存在(7093)` + 1 行 `不存在/闭源(2187)` + 1 行 `不存在(2170)` → PR URL 无效, 验证导入不崩
- 2 行 `占位假号(9999)` + 1 行 `占位假号(9998)` → 假 PR 号, 验证导入不崩
- 1 行 `正常但head为二进制(4447)` → 二进制 head, 验证导入不崩

---

## 3. 测试环境与工具

### 3.1 平台

- GZCTF: `http://10.77.0.10:8081` (docker compose, `harmony_ctf_platform-gzctf-1`)
- DB: PostgreSQL on `127.0.0.1:5434`, 库名 `gzctf`
- 默认管理员: `admin` / `Admin@2022`

### 3.2 浏览器驱动

- Skill 路径: `~/.claude/skills/gzctf-gameflow-test/`
- Driver: `scripts/drive.js` (daemon + unix socket, 长驻 Chromium)
- 3 个 daemon profile: `admin` / `playerA` / `playerB`, 各自独立 cookie 状态
- 原子操作: `goto / click / type / read / eval / wait / screenshot` 等, 详见 SKILL.md

### 3.3 关键约定

- **`goto` 后必 `wait`**: React SPA + wsrx 探针导致 networkidle 不 settle
- **不抽样**: 完整 59 行 xlsx 直接导入
- **不碰"测试赛"**: 已存在的比赛, 只操作新建的 `OH CTF E2E Full <ts>`
- **不清理**: 跑完不删比赛/工单, Game ID 写入报告留待人工清理
- **DB 直查**: `docker exec harmony_ctf_platform-db-1 psql -U gzctf -d gzctf -c "<query>"`

---

## 4. 规则核验矩阵 (R1–R30 → Phase)

| 规则 | 测试 Phase | 验证方式 | 期望结果 | 状态 |
|---|---|---|---|---|
| R1 0 人工 | N/A | — | — | N/A |
| R2 0day 评审组 | 4, 5 | admin daemon 审核 0day 工单 | admin = 评审组角色 | 待测 |
| R3 0day 奖金 | N/A | — | — | N/A |
| R4 Nday 已修复 | 1 | 导入数据集 (已修复漏洞) | 22 挑战入库 | 待测 |
| R5 动态分值/完成度 | 2.6 | L1 对 + L2 错 → 排行榜 +5 而非 0 | +5 | 待测 |
| R6 4 级 flag 链 | 1.8, 2.2-2.7 | admin Levels 页 + 玩家提交 | L1/L2/L3 三级可提交 | 待测 |
| R7 分值 5/4/1 | 1.8, 2.2-2.6 | admin 配置 + 玩家加分 | L1=5, L2=4, L3=1 | 待测 |
| R8 精确到代码行标记 | 2.11 | L1 格式边界测试 | 缺 `c:` / 缺行号 / 多空格 → 拒 | 待测 |
| R9 commit 语义 | 1.10, 2.5 | 注明数据集 commit = 引入还是修复 | 注明 | 待测 |
| R10 防爆破冻结 | 2.10 | 错答超限 → unlocksAt 锁定 | 锁定提示 | 待测 |
| R11 3 次取消资格 | 2.10.扩展 | 触发 3 次冻结 → 账号禁用 | 账号被禁 (若实现) | 待测 |
| R12-14 晋级名额 | N/A | — | — | N/A |
| R15 0day 专属入口 | 4.1 | `/games/$GAME_ID/zeroday-tickets` ≠ flag 提交 | 入口分离 | 待测 |
| R16 工单配额 | 4.6 | 持续提交到超限 | 第 N+1 单被拒 | 待测 |
| R17 4 严重度加分 | 4.1-4.4, 5.1 | 4 单 accept → 排行榜 +100/+60/+30/+10 | 加成正确 | 待测 |
| R18 误报申诉转 0day | 4.5, 5.2 | "类别不符" track L3 错 → 申诉 → accept | +100 (升级 Critical) | 待测 |
| R19 Nday ≥ 200 才能Keep 0day 加成 | 7 | TeamA Nday < 200 时排行榜 | 0day 加成扣减 (若实现) | 待测 (TBD) |
| R20 0day 判为数据集 Nday/无效 | 5.3, 5.4 | admin 把 0day 工单判为"实为 Nday" 或 "无效" | 0 分 | 待测 |
| R21 决赛指定仓库 | 4.7 | 0day 工单提交不在指定仓库的文件 | 应被拒 | 待测 |
| R22 不在仓库范围不得分 | 4.7, 5.5 | admin reject "not in dataset" | 0 分 | 待测 |
| R23 决赛总分公式 | N/A | — | 平台不实现 | N/A |
| R24 跨队重复不得分 | 3.2, 3.4, 6.3 | 双向 + 0day→Nday 后原提交者 | 0 分 | 待测 |
| R25 F1 Score | 7.3 | 排行榜/挑战页是否展示 F1 | 展示与否注明 | 待测 |
| R26 AI 使用 < 50% 不计分 | N/A | — | 评审主观, 平台不实现 | N/A |
| R27 提交 0 永久封禁 | 2.3, 2.4 | ban-trigger + post-ban | 永久封禁 | 待测 |
| R28 hash(0) 数据源 | 1.10 | DB 查 IsBanTrigger | 标记为 ban-trigger | 待测 |
| R29 类别不符申诉候选 | 4.5 | 选 sheet2 "类别不符" track | 申诉可提 | 待测 |
| R30 组合 CWE 顺序 | 2.6.扩展 | `CWE-787; CWE-125` 反序提交 | 看是否接受 | 待测 |

---

## 5. 8-Phase 测试流程

### Phase 0 — Setup & Admin Smoke (≈ 1 分钟) — 覆盖 R2

**目标**: 验活 + 3 daemon 启动 + admin 登录成功

| 步 | 动作 | 期望 | 覆盖规则 |
|---|---|---|---|
| 0.1 | `curl http://10.77.0.10:8081/` | HTTP 200 | — |
| 0.2 | `node drive.js daemon-start admin/playerA/playerB` | 3 socket 监听 | — |
| 0.3 | admin: `goto /account/Login` → `wait input[placeholder="ctfer"]` | 输入框可见 | — |
| 0.4 | 输入 `admin` / `Admin@2022` → 点 `登录` | URL 跳 `/` | R2 (admin=评审组角色) |
| 0.5 | 截图 `00-admin-login.png` | — | — |

**Pass**: URL=`/`, 无错误 toast。

---

### Phase 1 — Admin 完整建赛 + 59 行导入 (≈ 15 分钟) — 覆盖 R4, R6, R7, R9, R28

**目标**: 建比赛 + 全量导入 + 验证 22 挑战 + 三种 L1 格式 + 组合 CWE + hash(0) + 启动

| 步 | 动作 | 期望 | 覆盖规则 |
|---|---|---|---|
| 1.1 | `/admin/games` → 新建比赛 `OH CTF E2E Full <ts>`, 公开, 默认时间 | 弹窗关闭, 列表新增 | — |
| 1.2 | 抓 `GAME_ID` | 保存到 `$RUN_DIR/env` | — |
| 1.3 | `/admin/games/$GAME_ID/Info` → 设为 public → 保存 | 公开开关 ON | — |
| 1.4 | `/admin/games/$GAME_ID/import-flags` 上传**完整 59 行 xlsx** (cookie-reuse 脚本调 setInputFiles) | preview: 14 repos / 22 submodules / 59 tracks | R4 |
| 1.5 | 确认列映射: 仓库=所属仓库, 子模块=子模块, L1=文件路径(+代码行数), L2=commit id, L3=CWE(修复研判), MatchBy=FilePath | 自动检测正确 | R6 |
| 1.6 | 点 Import, 等 90s | `Imported=59, CreatedChallenges=22, Failed=0` | R4 |
| 1.7 | `/admin/games/$GAME_ID/challenges` | 列表 22 项 | R4 |
| 1.8 | 抽查 3 个挑战 (单行/范围/多行 L1 各一) 的 Levels 页 | 每条 track L1=5/L2=4/L3=1, flag 值与 xlsx 一致 | R6, R7 |
| 1.9 | 抽查 1 个组合 CWE track | L3 flag = `CWE-125; CWE-787` | R30 |
| 1.10 | 抽查 `hash(0)` track (commit=0) | L2 flag = `0`, DB 查 `"ChallengeFlags" where "Flag"='0'` 的 `IsBanTrigger` 字段 | R9, R28 |
| 1.11 | 抽查 1 个 `无法验证` commit 的 track | L2 为空或特殊值, 导入未崩 | R9 |
| 1.12 | 抽查 1 个 `占位假号` PR 的 track | 导入未崩, PR URL 字段为占位值 | — |
| 1.13 | 回 Info 页 → 比赛 Publish | 玩家侧 `/games/$GAME_ID` 可见 | — |
| 1.14 | 截图 `01-game-created.png` | — | — |

**Pass**: 22 挑战入库, 59 tracks, 三种 L1 格式 + 组合 CWE + hash(0) + 边界 case 全部验证, 比赛已启动。

---

### Phase 2 — PlayerA 多级 flag 全链路 (≈ 25 分钟) — 覆盖 R5-R11, R17, R27, R30

**目标**: 完整覆盖单选手所有 flag 提交场景 + 边界

#### 2.1 注册 + 建队 + 加入

| 步 | 动作 |
|---|---|
| 2.1.1 | playerA daemon: `/account/Register` 填 `playerA_<ts>` / `playerA_<ts>@e.test` / `Player@2024` |
| 2.1.2 | DB 置 `EmailConfirmed=true` (若需) |
| 2.1.3 | 登录 → `/account/Profile` 建队 `TeamA` |
| 2.1.4 | `/games/$GAME_ID` → 加入比赛 |

#### 2.2-2.11 提交矩阵

| 步 | 场景 | 动作 | 期望 | 覆盖规则 | 截图 |
|---|---|---|---|---|---|
| 2.2 | L1 单行格式 | 提交 `builtins_object.cpp:c:1060` | +5, L2 解锁 | R6, R7 | `02-L1-single.png` |
| 2.3 | L2 ban-trigger | 同 track 提交 `0` | 红色 ChallengeLockNotice, bannedLevel=2 | R27, R28 | `03-L2-ban.png` |
| 2.4 | L2 post-ban | 同 track 提交正确 commit | 仍被拒, "已封禁" | R27 | — |
| 2.5 | L1 范围格式 | 新 track 提交 `builtins_arraybuffer.cpp:c:550-559` | +5 | R6, R8 | — |
| 2.6 | **部分完成 (R5 动态分值)** | 同 2.5 track 提交**错误** L2 | L2 错, 但 L1 的 5 分保留 (排行榜仍 +5) | R5 | `04-partial.png` |
| 2.7 | L1 多行格式 | 新 track 提交 `builtins.cpp:c:281、c:286` | +5 | R6, R8 | `05-L1-multiline.png` |
| 2.8 | 干净链 L1→L2→L3 | 新 track (组合 CWE) 提交 L1 → L2 → L3=`CWE-125; CWE-787` | +5+4+1=+10, track 完整 | R5, R6, R7, R30 | `06-clean-chain.png` |
| 2.9 | **组合 CWE 顺序敏感性 (R30)** | 同 2.8 track 反序提交 `CWE-787; CWE-125` | 看是否接受 (Exact 应拒, Contains 应受) | R30 | `07-cwe-reversed.png` |
| 2.10 | 重复同级 | 已完成 track 再提 L1 | "已提交不得分" | R5 (首答得分) | — |
| 2.11 | **L1 格式严格性 (R8)** | 提交 `builtins_object.cpp:1060` (缺 `c:`) / `builtins_object.cpp:c:` (缺行号) / ` builtins_object.cpp:c:1060 ` (首尾空格) | 全部拒, 0 分 | R8 | `08-l1-format-strict.png` |
| 2.12 | 防爆破锁定 | 新 track 故意连错 N 次 (N=该级配额) | 锁定提示 + unlocksAt 倒计时 | R10 | `09-locked.png` |
| 2.13 | 数据集外 Nday | 提交 `fake/path.cpp:c:9999` | 拒, 0 分 | R20 (类似) | — |
| 2.14 | **3 次冻结取消资格 (R11, 若实现)** | 若 2.12 已触发一次冻结, 重复 2 次 (跨日或同日模拟) | 第 3 次后账号被禁, 无法提交 | R11 | `10-account-disabled.png` |

**Phase 2 Pass**: 13 个子场景全部按期望返回 (2.14 若平台未实现则跳过并注明)。

---

### Phase 3 — PlayerB 跨队重复不得分 (≈ 5 分钟) — 覆盖 R24

**目标**: 验证"同一漏洞仅第一个团队得分, 后续重复不得分"双向

| 步 | 动作 | 期望 | 覆盖规则 | 截图 |
|---|---|---|---|---|
| 3.1 | playerB daemon 注册 `playerB_<ts>` + 建队 `TeamB` + 加入同比赛 | OK | — | — |
| 3.2 | playerB 找 playerA **已解决** 的 track (2.2 的 `c:1060`), 提交相同 L1 | **0 分**, "重复, 已被他队提交" | R24 | `11-duplicate-cross.png` |
| 3.3 | playerB 找 playerA **未解决** 的 track, 提交正确 L1 | +5 (B 队先得分) | R24 | — |
| 3.4 | playerA 回头提交 B 队刚解决的 track (3.3) | **0 分**, 重复 | R24 (双向) | — |
| 3.5 | playerB 完整提交 3.3 track 的 L2+L3 | +4+1=+5 (B 队继续得分) | R5, R7 | — |

**Phase 3 Pass**: 跨队重复双向都生效。

---

### Phase 4 — PlayerA 0day 工单 × 4 + 误报申诉 + 边界 (≈ 15 分钟) — 覆盖 R15-R18, R20-R22, R29

**目标**: 覆盖 0day 工单 4 严重度 + 误报申诉 + 工单超量 + 数据集外 + 不在指定仓库

#### 4.1 确认 0day 专属入口 (R15)

| 步 | 动作 | 期望 |
|---|---|---|
| 4.1.1 | playerA: `/games/$GAME_ID/zeroday-tickets` | 0day 工单页面, 与 `/games/$GAME_ID/Challenges` (flag 提交) 是不同入口 |
| 4.1.2 | 检查工单表单字段 | 至少含: 标题 / 严重度 / 模块(仓库) / 文件路径 / 代码行 / commit / CWE / PoC / 描述 |

#### 4.2 提交 4 严重度 0day 工单 (R17)

| # | 标题 | 严重度 | 模块 | 文件:line | commit | CWE | PoC | 期望 | 截图 |
|---|---|---|---|---|---|---|---|---|---|
| T1 | E2E 0day Critical | 严重/Critical | (任一已导入仓库) | `fake/crit.cpp:c:100` | `aaa...` (假) | CWE-787 | 假 PoC | Pending | `12-0day-crit.png` |
| T2 | E2E 0day High | 高危/High | (不同仓库) | `fake/high.cpp:c:200` | `bbb...` | CWE-476 | 假 PoC | Pending | `12-0day-high.png` |
| T3 | E2E 0day Medium | 中危/Medium | (不同仓库) | `fake/med.cpp:c:300` | `ccc...` | CWE-125 | 假 PoC | Pending | `12-0day-med.png` |
| T4 | E2E 0day Low | 低危/Low | (不同仓库) | `fake/low.cpp:c:400` | `ddd...` | CWE-20 | 假 PoC | Pending | `12-0day-low.png` |

每单抓 `TICKET_ID`, 写入 `$RUN_DIR/env`。

#### 4.3 误报申诉 (R18, R29)

| 步 | 动作 | 期望 |
|---|---|---|
| 4.3.1 | 从 sheet2 "终检核对" 选 1 个 `类别不符` 的 track (如 C2/C3/C4/C5) | — |
| 4.3.2 | playerA 提交该 track 的 L3 = `CWE(原标)` 值 (如 CWE-200) | 被拒 (因为正确答案是 `CWE(修复研判)`) |
| 4.3.3 | playerA 提 0day 工单: 标题 `误报申诉 — track <X>`, 严重度 High, 模块=该 track 仓库, 文件:line/commit/CWE 全填, 描述说明类别不符 | Pending |
| 4.3.4 | 抓 `APPEAL_TICKET_ID` | — |

截图 `13-appeal-filed.png`。

#### 4.4 工单超量 (R16)

| 步 | 动作 | 期望 |
|---|---|---|
| 4.4.1 | 持续提交 0day 工单 (假内容) | 每单 Pending |
| 4.4.2 | 直到第 N+1 单被拒 | "超出工单限额" 提示, 记录 N 值 |
| 4.4.3 | DB 查工单配额配置 (若有 `TicketQuota` 表/字段) | — |

截图 `14-quota-exceeded.png`。

#### 4.5 不在指定仓库 (R21, R22)

| 步 | 动作 | 期望 |
|---|---|---|
| 4.5.1 | playerA 提 0day 工单, 模块填一个**不在 14 个已导入仓库**的 (如 `random_repo`) | 提交时被拒, 或 admin 审核时拒 |
| 4.5.2 | 记录平台行为 (前端拒 vs 后端拒) | — |

截图 `15-out-of-repo.png`。

#### 4.6 数据集外 Nday 误作 0day (R20 边界)

| 步 | 动作 | 期望 |
|---|---|---|
| 4.6.1 | playerA 提 0day 工单, 内容其实是**数据集里已有的 Nday** (file:line/commit/CWE 全用 xlsx 真值) | Pending (玩家企图用 0day 入口重提已知的 Nday) |
| 4.6.2 | Phase 5.3 admin 审核时判为"实为数据集 Nday" → 0 分 | — |

**Phase 4 Pass**: 4 严重度 + 申诉 + 超量 + 不在仓库 + 数据集外误作 0day 全部覆盖。

---

### Phase 5 — Admin 审核 0day 工单 (≈ 15 分钟) — 覆盖 R2, R17-R22, R24

**目标**: 审核所有工单, 验证加分 + 0day 入数据集 + 各类拒绝

| 工单 | 动作 | 期望 TeamA 加分 | 覆盖规则 | 截图 |
|---|---|---|---|---|
| T1 Critical | Accept, 严重度保持 Critical | +100 | R17 | `16-accept-crit.png` |
| T2 High | Accept | +60 | R17 | `16-accept-high.png` |
| T3 Medium | Accept | +30 | R17 | `16-accept-med.png` |
| T4 Low | Accept | +10 | R17 | `16-accept-low.png` |
| 4.3 误报申诉 | Accept **+ 升级为 Critical 0day** | +100 | R18 | `17-appeal-accepted.png` |
| 4.6.1 数据集 Nday 误作 0day | Reject, 理由"实为数据集已有 Nday" | 0 | R20 | `18-reject-isnday.png` |
| 4.5.1 不在指定仓库 | Reject, 理由"不在指定代码仓范围" | 0 | R21, R22 | `18-reject-outofrepo.png` |
| (额外) 任挑一单 (如 T3) | Reject, 理由"无效漏洞" | 0 (但 T3 已 accept, 选另一个) | R20 | `18-reject-invalid.png` |

#### 5.1 验证 0day 入数据集 (R18 闭环关键)

```bash
docker exec harmony_ctf_platform-db-1 psql -U gzctf -d gzctf \
  -c "select \"Id\",\"Repository\",\"FilePath\" from \"Tracks\" \
      where \"FilePath\" like '<appeal-file-path>' order by \"Id\" desc limit 5;"
```
或 admin UI 进对应挑战, 确认新 Track 存在, L1/L2/L3 flag 来自工单内容。

#### 5.2 验证玩家侧状态同步

playerA / playerB daemon 分别进 `/games/$GAME_ID/zeroday-tickets`, 确认每单状态 (Accepted/Rejected) 与 admin 操作一致。

截图 `19-tickets-synced.png`。

**Phase 5 Pass**: 4 严重度 accept + 申诉 accept+升级+入数据集 + 3 类 reject (数据集 Nday/不在仓库/无效) + 玩家侧同步。

---

### Phase 6 — 0day → Nday 闭环 (≈ 5 分钟) — 覆盖 R18, R24

**目标**: 验证审核通过的 0day 被加入数据集后, 其他选手可挖此 Nday, 原提交者不得分

| 步 | 动作 | 期望 | 覆盖规则 | 截图 |
|---|---|---|---|---|
| 6.1 | playerB 找到 Phase 5.1 入库的新 Nday Track | — | R18 | — |
| 6.2 | playerB 提交 L1 (file:line) → L2 (commit) → L3 (CWE) 全部正确 | +5+4+1 = **+10** | R5, R7 | `20-new-nday-solved.png` |
| 6.3 | playerA (原 0day 提交者) 提交同一新 Nday 的 L1 | **0 分**, 重复 | R24 | `21-duplicate-original.png` |
| 6.4 | playerB 提交后再来一次同 L1 | **0 分**, 重复 (自身) | R5 | — |

**Phase 6 Pass**: 闭环成立 — 0day 变 Nday, 其他人能得分, 原提交者不再得分。

---

### Phase 7 — 排行榜 + 报告 (≈ 10 分钟) — 覆盖 R5, R7, R17-R20, R24, R25, R27

#### 7.1 排行榜验证

`/games/$GAME_ID/Scoreboard`:

| 队伍 | Nday 得分 | 0day 加成 | 总分 | 排名 |
|---|---|---|---|---|
| TeamA | ~25 (干净链 10 + 单 L1 5 + 多 L1 5 + 部分 5) | 100+60+30+10+100=300 | **~325** | #1 |
| TeamB | ~20 (Phase 3.3+3.5 的 10 + Phase 6.2 的 10) | 0 | **~20** | #2 |

(具体数值以实际提交为准, 报告写真实值)

#### 7.2 R19 验证 — Nday ≥ 200 才能Keep 0day 加成

- TeamA Nday ≈ 25 < 200, 若平台执行 R19, 则 0day 加成 300 应被扣减
- **在报告里注明**: 平台是否实现 R19 规则
- DB 查找相关配置/字段: `select * from "GameScorePolicy" where "GameId"=$GAME_ID;` (表名待确认)

#### 7.3 R25 验证 — F1 Score 展示

- 检查排行榜页/挑战页是否展示"漏洞定位 F1" + "漏洞验证 F1" 指标
- 检查 admin 是否能看到全队提交的精准率/召回率
- **在报告里注明**: 平台是否实现 R25 指标展示

#### 7.4 R27 验证 — ban-trigger 永久性

- DB 查 playerA 在 Phase 2.3 触发 ban 的 track, 验证 `TeamTrackState` (或类似表) 中 `BannedLevel=2` 永久标记
- 即使比赛重启, ban 状态应保留

#### 7.5 收尾

1. `daemon-stop admin/playerA/playerB`
2. 在 CWD 写 `gzctf-gameflow-report.md`, 含:
   - 每阶段 PASS/FAIL
   - 数据集覆盖矩阵 (59 行 / 三种 L1 格式 / 组合 CWE / hash(0) / 无法验证 / 占位假号)
   - **规则核验矩阵** (R1-R30 每条 → Phase → 验证结果)
   - 所有截图引用
   - 遇到的问题与重现
   - 平台未实现的规则 (R11/R19/R23/R25/R26 等) 标注

---

## 6. 风险与已知边界

| 风险 | 应对 | 关联规则 |
|---|---|---|
| xlsx 上传 UI 选择器变化 | 用 cookie-reuse 脚本调 setInputFiles, 失败则直接调 preview API | — |
| L1 答案实际格式 (是否拼接 file:line) | Phase 1.8 抽查确认, 若不拼接则 Phase 2 的 L1 答案相应调整 | R6, R8 |
| `hash(0)` 行未被后端标记为 ban-trigger | Phase 1.10 DB 查 `IsBanTrigger`, 若未标记则 Phase 2.3 用 admin 配置一个 ban-trigger, 或跳过 2.3 并在报告标 PARTIAL | R27, R28 |
| 0day accept 不自动入数据集 | Phase 5.1 DB 查新 Track, 若未自动入则 0day→Nday 闭环标 FAIL, Phase 6 跳过 | R18 |
| 防爆破次数限制未配置 | Phase 2.12 若无锁定反应, 跳过并在报告注明 | R10 |
| 3 次冻结取消资格未实现 | Phase 2.14 跳过, 报告标 N/A | R11 |
| R19 Nday≥200 未实现 | Phase 7.2 注明, 报告标 N/A | R19 |
| R25 F1 Score 未实现 | Phase 7.3 注明, 报告标 N/A | R25 |
| 邮箱验证阻塞注册 | DB 直接置 `EmailConfirmed=true` | — |
| React 渲染时机 | 每个 `goto` 后必 `wait` 具体元素 | — |
| "测试赛" 误操作 | 全程只操作 `OH CTF E2E Full <ts>`, 不点已存在比赛 | — |
| 组合 CWE 顺序敏感 (Exact vs Contains) | Phase 2.9 测反序, 记录 answerType 行为 | R30 |
| 0day 工单 PoC 字段是否必填 | Phase 4.2 提交时填, Phase 4.6 可测空 PoC 是否被拒 | R20 |
| 比赛时间窗口外提交 | 若结束时间已过, 提交应被拒 — 可在 Phase 7 后用 admin 提前结束比赛验证 | — |
| 0day 严重度 admin 可否修改 | Phase 5 accept 申诉时尝试升级 Critical → High, 看是否允许 | R17 |
| 多 track 同时进行的 trackId 缓存隔离 | Phase 2 切换 track 时验证 trackId 不串 | R6 |

---

## 7. 执行 Checklist

### Phase 0
- [ ] 0.1 curl 验活
- [ ] 0.2 3 daemon 启动
- [ ] 0.3-0.5 admin 登录 + 截图

### Phase 1
- [ ] 1.1-1.3 建赛 + 抓 GAME_ID + 设 public
- [ ] 1.4-1.6 上传完整 59 行 xlsx + Import
- [ ] 1.7 22 挑战列表
- [ ] 1.8 三种 L1 格式抽查
- [ ] 1.9 组合 CWE 抽查
- [ ] 1.10 hash(0) + IsBanTrigger DB 查
- [ ] 1.11 无法验证 commit 抽查
- [ ] 1.12 占位假号 PR 抽查
- [ ] 1.13-1.14 启动比赛 + 截图

### Phase 2
- [ ] 2.1 注册 + 建队 + 加入
- [ ] 2.2 L1 单行
- [ ] 2.3 L2 ban-trigger
- [ ] 2.4 L2 post-ban
- [ ] 2.5 L1 范围
- [ ] 2.6 部分完成 (L1对L2错)
- [ ] 2.7 L1 多行
- [ ] 2.8 干净链 + 组合 CWE
- [ ] 2.9 组合 CWE 反序
- [ ] 2.10 重复同级
- [ ] 2.11 L1 格式严格性 (3 子 case)
- [ ] 2.12 防爆破锁定
- [ ] 2.13 数据集外 Nday
- [ ] 2.14 3 次冻结 (若实现)

### Phase 3
- [ ] 3.1 playerB 注册 + 建队 + 加入
- [ ] 3.2 B 提交 A 已解决 → 0 分
- [ ] 3.3 B 提交 A 未解决 → 得分
- [ ] 3.4 A 回头提交 B 解决的 → 0 分
- [ ] 3.5 B 完整链 L2+L3

### Phase 4
- [ ] 4.1 0day 专属入口确认
- [ ] 4.2 T1-T4 四严重度工单
- [ ] 4.3 误报申诉 (类别不符 track)
- [ ] 4.4 工单超量
- [ ] 4.5 不在指定仓库
- [ ] 4.6 数据集 Nday 误作 0day

### Phase 5
- [ ] 5.1 T1-T4 accept (Critical/High/Medium/Low)
- [ ] 5.2 申诉 accept + 升级 Critical + 入数据集
- [ ] 5.3 数据集 Nday 误作 0day reject
- [ ] 5.4 不在指定仓库 reject
- [ ] 5.5 无效漏洞 reject
- [ ] 5.6 玩家侧状态同步验证

### Phase 6
- [ ] 6.1 playerB 找新 Nday track
- [ ] 6.2 playerB 完整链 → +10
- [ ] 6.3 playerA 回头 → 0 分
- [ ] 6.4 playerB 重复 → 0 分

### Phase 7
- [ ] 7.1 排行榜 + 加分核对
- [ ] 7.2 R19 Nday≥200 验证
- [ ] 7.3 R25 F1 Score 验证
- [ ] 7.4 R27 ban 永久性 DB 验证
- [ ] 7.5 daemon 停止 + 报告写入

---

## 8. 与 SKILL.md 的关系

- **SKILL.md** (`~/.claude/skills/gzctf-gameflow-test/SKILL.md`): 描述 driver、命令、selectors、操作指南 — "怎么操作浏览器"
- **本文档** (`docs/test.md`): 描述测试计划、覆盖点、规则核验 — "测什么、怎么验"

两份文档保持同步。selectors 变了改 SKILL.md, 测试覆盖点变了改本文档。
规则条目 (R1-R30) 是本文档的索引骨架, SKILL.md 的 Phase 编号与本文档一致。

---

## 9. 报告模板 (gzctf-gameflow-report.md)

```markdown
# GZCTF Game-Flow E2E Report — <timestamp>

## Summary
- Base URL: <BASE_URL>
- Game ID: <id>, Title: <title>
- Run dir: /tmp/gzctf-run-<ts>
- Dataset: 59 rows / 14 repos / 22 challenges
- Players: TeamA (playerA_<ts>), TeamB (playerB_<ts>)

## Phase 0 — Admin Login
- Status: PASS / FAIL
- Screenshot: ![](00-admin-login.png)

## Phase 1 — Admin Setup (Full 59-row Import)
- Game created: id=<id>, title=<title>
- Imported: 59/59 (CreatedChallenges=22, Failed=0)
- L1 format coverage: single ✓ / range ✓ / multi ✓
- Combined CWE track verified: yes/no
- hash(0) ban-trigger source: IsBanTrigger=yes/no (DB query result)
- 无法验证 commit track: imported without crash ✓
- 占位假号 PR track: imported without crash ✓
- Game started: yes/no

## Phase 2 — Player A Submission Matrix
| Step | Scenario | Expected | Actual | Pass | Screenshot |
|---|---|---|---|---|---|
| 2.2 | L1 single `c:1060` | +5 | +5 | ✓ | 02-... |
| 2.3 | L2 ban-trigger `0` | banned | banned | ✓ | 03-... |
| 2.4 | L2 post-ban | rejected | rejected | ✓ | — |
| 2.5 | L1 range `c:550-559` | +5 | +5 | ✓ | — |
| 2.6 | Partial (L1 ok L2 wrong) | +5 retained | +5 | ✓ | 04-... |
| 2.7 | L1 multi `c:281、c:286` | +5 | +5 | ✓ | 05-... |
| 2.8 | Clean chain + combined CWE | +10 | +10 | ✓ | 06-... |
| 2.9 | Combined CWE reversed | TBD | ... | ? | 07-... |
| 2.10 | Duplicate same-level | rejected | ... | ? | — |
| 2.11 | L1 format strict (3 cases) | all rejected | ... | ? | 08-... |
| 2.12 | Anti-brute lockout | locked | ... | ? | 09-... |
| 2.13 | Out-of-dataset Nday | rejected | ... | ? | — |
| 2.14 | 3 strikes disqualify | disabled | N/A | N/A | — |

## Phase 3 — Player B Cross-Team Duplicate
| Step | Expected | Actual | Pass |
|---|---|---|---|
| 3.2 B→A's solved | 0 pts | ... | ? |
| 3.3 B→A's unsolved | +5 | ... | ? |
| 3.4 A→B's solved | 0 pts | ... | ? |
| 3.5 B full chain | +5 (L2+L3) | ... | ? |

## Phase 4 — 0day Tickets
- T1 Critical: id=<id> — screenshot
- T2 High: id=<id> — screenshot
- T3 Medium: id=<id> — screenshot
- T4 Low: id=<id> — screenshot
- Appeal (类别不符 track): id=<id> — screenshot
- Quota limit: <N>, exceeded at ticket #<N+1>
- Out-of-repo ticket: id=<id> — behavior: <frontend-rejected/backend-rejected>
- Dataset-Nday-as-0day ticket: id=<id>

## Phase 5 — Admin Review
| Ticket | Action | Bonus | Pass |
|---|---|---|---|
| T1 Critical | Accept | +100 | ? |
| T2 High | Accept | +60 | ? |
| T3 Medium | Accept | +30 | ? |
| T4 Low | Accept | +10 | ? |
| Appeal | Accept+Upgrade Critical | +100 | ? |
| Dataset-Nday-as-0day | Reject (is-Nday) | 0 | ? |
| Out-of-repo | Reject (out-of-repo) | 0 | ? |
| Invalid | Reject (invalid) | 0 | ? |
- 0day entered dataset (DB verified): yes/no
- Player-side status sync: yes/no

## Phase 6 — 0day → Nday Closed Loop
- playerB solved new Nday chain: +10 — PASS/FAIL
- playerA re-submit rejected as duplicate: PASS/FAIL
- playerB self-duplicate rejected: PASS/FAIL

## Phase 7 — Scoreboard
- Final ranking: TeamA #1 (<score>), TeamB #2 (<score>)
- Score breakdown table
- R19 (Nday≥200 to keep 0day bonus): enforced / not enforced / N/A
- R25 (F1 Score display): displayed / not displayed / N/A
- R27 (ban permanence DB verified): yes/no

## Rule Compliance Matrix (R1-R30)
| Rule | Phase | Verified | Notes |
|---|---|---|---|
| R1 0-人工 | N/A | N/A | 平台无法验证 |
| R2 0day 评审组 | 4,5 | ✓ | admin=评审组 |
| R3 0day 奖金 | N/A | N/A | 资金流平台不实现 |
| R4 Nday 已修复 | 1 | ✓ | 59 行已修复漏洞导入 |
| R5 动态分值/完成度 | 2.6 | ? | L1对L2错→+5保留 |
| R6 4 级 flag 链 | 1.8, 2.2-2.7 | ? | L1/L2/L3 可提交 |
| R7 分值 5/4/1 | 1.8, 2.x | ? | 加分验证 |
| R8 精确到代码行 | 2.11 | ? | 格式严格性 |
| R9 commit 语义 | 1.10 | ? | 注明引入vs修复 |
| R10 防爆破冻结 | 2.12 | ? | unlocksAt |
| R11 3 次取消资格 | 2.14 | ? | 若实现 |
| R12-14 晋级名额 | N/A | N/A | 平台不实现 |
| R15 0day 专属入口 | 4.1 | ? | 入口分离 |
| R16 工单配额 | 4.4 | ? | 超量被拒 |
| R17 4 严重度加分 | 4.2, 5.1 | ? | +100/60/30/10 |
| R18 误报申诉转 0day | 4.3, 5.2 | ? | accept+升级 |
| R19 Nday≥200 | 7.2 | ? | TBD |
| R20 0day 判为 Nday/无效 | 5.3, 5.5 | ? | 0 分 |
| R21 决赛指定仓库 | 4.5 | ? | 不在仓库被拒 |
| R22 不在范围不得分 | 4.5, 5.4 | ? | 0 分 |
| R23 决赛总分公式 | N/A | N/A | 平台不实现 |
| R24 跨队重复 | 3.2, 3.4, 6.3 | ? | 双向+闭环 |
| R25 F1 Score | 7.3 | ? | 展示与否 |
| R26 AI<50% 不计分 | N/A | N/A | 评审主观 |
| R27 提交0永久封禁 | 2.3, 2.4 | ? | ban+post-ban |
| R28 hash(0) 数据源 | 1.10 | ? | IsBanTrigger |
| R29 类别不符申诉 | 4.3 | ? | 候选已选 |
| R30 组合 CWE 顺序 | 2.9 | ? | 反序行为 |

## Issues Encountered
- (each issue with reproduction, screenshot, suspected cause)

## Platform Gaps (Rules Not Implemented)
- R11 3 次取消资格: <implemented / not / unknown>
- R19 Nday≥200: <implemented / not / unknown>
- R23 决赛总分公式: <implemented / not / unknown>
- R25 F1 Score: <implemented / not / unknown>
- R26 AI<50%: <implemented / not / unknown> (评审主观, 不期望实现)

## Artifacts
- Screenshots: /tmp/gzctf-run-<ts>/*.png
- Preview/import responses: /tmp/gzctf-run-<ts>/*.json
- This report: ./gzctf-gameflow-report.md
```
