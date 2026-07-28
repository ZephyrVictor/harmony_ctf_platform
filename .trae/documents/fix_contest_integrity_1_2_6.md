# 比赛完整性问题 #1、#2、#6 修复计划

## Summary

本计划修复审计报告中的三个已选问题：

1. `TrackId` 未绑定当前题目，允许跨题目引用 Flag 并按当前题目计分。
2. 报名流程缺少 `(TeamId, GameId)` 唯一约束和并发串行化，同队成员并发报名可创建重复 `Participation` 或突破成员上限。
3. `Exact` 类型不支持比赛规则要求的组合 CWE 任一命中，例如 `CWE-125; CWE-787`。

完成标准：

- 非法、外题或不符合当前 Level 的 `TrackId` 返回 HTTP 400，不创建提交、不扣分、不累计冷却。
- 同一战队在同一比赛中最多存在一条 `Participation`；同队并发报名不会突破成员上限。
- `Exact` 标准答案先保留整串精确匹配，再按该 Level 的 `OrSeparators` 支持任一完整候选项匹配。
- 单元测试、本地 PostgreSQL 集成测试、K3s/MinIO 云模式集成测试、Chrome 竞赛流程全部通过。
- 更新 E2E 测试计划、结构化结果与必要截图；trace 只保存在本地。

## Current State Analysis

### #1 TrackId 越界

- `FlagSubmitModel` 允许客户端直接提交 `TrackId` 和 `Level`。
- `ScoringService.ProcessSubmission` 在有 `TrackId` 时调用 `GetFlagsByTrackLevel(trackId, level)`。
- `ChallengeFlagRepository` 只过滤 `TrackId + Level + IsEnabled`，没有过滤当前 `ChallengeId`。
- 正确答案命中后，系统使用当前题目的 Level 配置计算分数，并把外题 `MatchedFlagId/TrackId` 写入当前题目的 `SubmissionRecord`。
- 正常前端 L1 不传 `TrackId`，L2+ 才传当前 Track，因此可以严格校验而不破坏现有正常流程。

### #2 报名竞态

- `JoinGame` 在普通 Read Committed 事务中先查询 `Participation` 和成员数量，再创建记录。
- `Participation` 的 `(TeamId, GameId)` 当前只是普通索引。
- 两名同队成员并发首次报名时可以同时读到 `part == null`，分别创建参赛记录。
- `(UserId, GameId)` 已有唯一索引，但只能阻止同一用户跨队并发，不能阻止同队不同成员并发。
- 项目已有 PostgreSQL transaction advisory lock 的使用先例，可沿用该机制。

### #6 Exact 组合答案

- 导入映射和 `ChallengeLevelConfig` 已包含 `OrSeparators`，默认值为 `、,;`。
- 导入时 `OrSeparators` 已写入 Level 配置，但 `TryMatch` 对 `Exact` 仅执行整串比较。
- 现有数据包含 `CWE-125; CWE-787`，因此只提交 `CWE-125` 或 `CWE-787` 均无法命中。
- 采用匹配时解析可以立即兼容存量数据，不要求重新导入或修改现有 Flag。

## Scope

### In Scope

- 修复 #1、#2、#6 的后端逻辑、数据库约束、管理端导入配置入口。
- 添加数据库迁移和上线前重复数据预检。
- 恢复单元及集成测试基线。
- 添加针对对象越界、并发报名、成员上限和 Exact OR 的自动化测试。
- 执行四层全量验证并更新 E2E 证据。

### Out of Scope

- 审计报告中的 #3、#4、#5、#7、#8、#9、#10。
- 自动合并已有重复 `Participation`。
- 改变现有冷却、Trap、0day 或排行榜规则。
- 将当前人工编排的 Chrome E2E 重构为新的 Playwright 测试框架。

## Assumptions & Decisions

- 非法 Track 上下文返回 HTTP 400，不产生任何提交、处罚或冷却计数。
- L1 必须不带 `TrackId`；L2+ 必须携带属于当前题目且启用的 Track。
- `Exact` OR 匹配先比较完整标准答案，再比较由 `OrSeparators` 拆出的完整候选项。
- 拆分时去除候选项首尾空白、忽略空项，并继续遵守 `CaseSensitive`。
- 不对提交内容再做二次拆分；提交必须等于整串标准答案或其中一个完整候选项。
- Exact 标准答案包含已配置分隔符时，其语义明确视为 OR；这是已确认的兼容取舍。
- 存量重复 `Participation` 只做预检并中止迁移，不自动合并积分、实例或状态。
- 报名锁固定按“用户-比赛锁”再“战队-比赛锁”的顺序获取，避免死锁。
- 测试基线完整恢复，包括单测项目识别和所有集成测试报名请求的承诺书字段。

## Proposed Changes

### 1. 收紧 Track/Level 信任边界

涉及文件：

- `src/GZCTF/Repositories/Interface/IChallengeTrackRepository.cs`
- `src/GZCTF/Repositories/ChallengeTrackRepository.cs`
- `src/GZCTF/Repositories/Interface/IChallengeFlagRepository.cs`
- `src/GZCTF/Repositories/ChallengeFlagRepository.cs`
- `src/GZCTF/Models/Request/Game/FlagSubmitModel.cs`
- `src/GZCTF/Services/Scoring/ScoringService.cs`
- `src/GZCTF/Resources/Program*.resx`

实现：

1. 为 `FlagSubmitModel.Level` 增加正整数模型校验。
2. 为 Track 仓储增加按 `ChallengeId + TrackId` 查询启用 Track 的方法。
3. 将 Flag 仓储的 Track-Level 查询改为同时接收并过滤 `ChallengeId + TrackId + Level`。
4. 在 `ProcessSubmission` 任何锁、封禁、冷却和计分状态变更之前校验：
   - `Level >= 1`；
   - L1 不允许携带 `TrackId`；
   - L2+ 必须携带 `TrackId`；
   - Track 必须存在、启用且属于当前题目；
   - 请求的多级 Level 必须存在且启用。
5. 为这些错误增加全部现有语言的资源项，校验失败抛出已有 `InvalidFlagSubmissionException`，由 `GameController.Submit` 回滚并返回 400。
6. 后续候选 Flag 查询继续携带当前 `ChallengeId`，形成第二层防线。
7. 保持合法 L1/L2/L3、封禁、冷却和重复提交响应不变。

### 2. 使报名创建和成员上限检查原子化

涉及文件：

- `src/GZCTF/Repositories/Interface/IParticipationRepository.cs`
- `src/GZCTF/Repositories/ParticipationRepository.cs`
- `src/GZCTF/Controllers/GameController.cs`
- `src/GZCTF/Models/Data/Participation.cs`
- `src/GZCTF/Migrations/<timestamp>_EnforceUniqueParticipationPerTeamGame.cs`
- `src/GZCTF/Migrations/<timestamp>_EnforceUniqueParticipationPerTeamGame.Designer.cs`
- `src/GZCTF/Migrations/AppDbContextModelSnapshot.cs`

实现：

1. 在 `IParticipationRepository` 增加 `AcquireJoinLocks(gameId, teamId, userId)`。
2. 仓储在当前事务内依次获取两个 PostgreSQL transaction advisory lock：
   - `(gameId, userId)`：串行化同一用户对同一比赛的跨队报名；
   - `(gameId, teamId)`：串行化同一战队的创建和成员计数。
3. `JoinGame` 在获得锁后重新查询：
   - 用户是否已通过其他战队参赛；
   - 当前战队的 `Participation`；
   - 当前参赛成员数量。
4. 在锁内完成创建/复用 Participation、成员上限检查、成员添加、自动审核和提交事务。
5. 对唯一约束冲突统一返回 HTTP 409，不返回 500；普通成员上限仍返回现有 400。
6. 通过 `Participation` 的 Index 特性将现有 `(TeamId, GameId)` 索引改为唯一索引，作为跨进程和非标准写入路径的最终约束。

迁移策略：

1. `Up` 首先执行只读式重复检测：

   ```sql
   SELECT "GameId", "TeamId", array_agg("Id" ORDER BY "Id")
   FROM "Participations"
   GROUP BY "GameId", "TeamId"
   HAVING count(*) > 1;
   ```

2. 若存在结果，迁移通过 `RAISE EXCEPTION` 明确中止并给出人工检查提示。
3. 无重复时，将 `IX_Participations_TeamId_GameId` 重建为唯一索引。
4. `Down` 恢复为普通索引，不删除任何业务数据。
5. 正式部署前单独执行同一预检 SQL；结果非空时停止发布，由人工确认主记录及关联数据后另行处理。

### 3. 为 Exact 增加 OR 候选匹配

涉及文件：

- `src/GZCTF/Services/Scoring/ScoringService.cs`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/challenges/[chalId]/Import.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/import-flags.tsx`
- `src/GZCTF/ClientApp/src/pages/admin/games/[id]/challenges/[chalId]/Levels.tsx`

实现：

1. 抽取 `TryMatchExact`：
   - 先按当前 `CaseSensitive` 设置比较完整 `FlagValue`；
   - 完整值不匹配且 `OrSeparators` 非空时，按其字符集合拆分标准答案；
   - 至少产生两个非空候选项时，逐项执行完整字符串比较；
   - 不做包含、前缀、模糊或顺序重排匹配。
2. `TryMatch` 的 `Exact` 分支改用该方法；其他 AnswerType 不变。
3. 不修改存量 Flag，不新增数据迁移，不改变导入文件中的原始组合值。
4. 题目级和比赛级导入页面：
   - `Exact` 和 `SourceLocation` 均展示 `OrSeparators`；
   - `RangeSeparator`、路径大小写和行号前缀配置仍只属于 `SourceLocation`；
   - 默认分隔符保持 `、,;`。
5. Level 编辑页继续复用现有 `OrSeparators` 字段，并调整说明文本，使其同时描述 Exact 候选答案和 SourceLocation 行号 OR。

### 4. 恢复测试基线

涉及文件：

- `src/GZCTF.Test/GZCTF.Test.csproj`
- `src/GZCTF.Integration.Test/Tests/Api/*.cs` 中所有 `GameJoinModel` 调用点
- 必要的测试辅助方法

实现：

1. 为 `GZCTF.Test.csproj` 增加 `<IsTestProject>true</IsTestProject>`，确保默认 `dotnet test` 不再静默跳过。
2. 将现有 79 处测试报名请求按测试意图补充 `CommitmentAccepted = true`。
3. 新增一个明确验证 `CommitmentAccepted=false` 返回规则错误的测试，防止恢复基线时削弱生产门槛。
4. 不修改生产 DTO 的默认值。

### 5. 添加定向自动化测试

#### #1 TrackId

文件：

- `src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagFlowTests.cs`
- 必要时扩展测试 Fixture

覆盖：

- L1 携带任意 TrackId 返回 400，提交数、分数、冷却均不变化。
- L2 使用同比赛另一题目的 TrackId 和正确答案返回 400。
- L2 使用其他比赛 TrackId 返回 400。
- 禁用 Track 和不存在 Track 返回 400。
- 合法当前题目 Track 的 L2/L3 流程仍成功。

#### #2 并发报名

文件：

- `src/GZCTF.Integration.Test/Tests/Api/GameJoinTests.cs`
- `src/GZCTF.Integration.Test/Tests/Database/DatabaseContextTests.cs`

覆盖：

- 同队两名成员并发首次报名：两个合法请求最终共享一条 Participation。
- 成员上限为 2 时，已有一人后两人并发报名：仅一人成功，最终成员数严格为 2。
- 同一用户对两个战队并发报名：最多一个成功，数据库保持 `(UserId, GameId)` 唯一。
- 直接插入同一 `(TeamId, GameId)` 的第二条 Participation 被唯一索引拒绝。
- 普通串行报名、审核、拒绝后重报和承诺书校验保持原行为。

#### #6 Exact OR

文件：

- `src/GZCTF.Test/UnitTests/Scoring/ScoringServiceTests.cs`
- `src/GZCTF.Test/UnitTests/Import/FlagImportModelsTests.cs`
- `src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagImportTests.cs`
- `src/GZCTF.Integration.Test/Tests/Api/MultiLevelFlagFlowTests.cs`

覆盖：

- `CWE-125; CWE-787` 接受完整整串、`CWE-125` 和 `CWE-787`。
- 不接受 `CWE-12`、`CWE-125;` 的部分或未知候选。
- `CaseSensitive=true/false` 均按配置工作。
- 自定义分隔符如 `|` 生效；空分隔符保持纯 Exact。
- 导入后的组合 Flag 保持单条原始值，LevelConfig 正确保留 `OrSeparators`。
- 玩家完成 L1/L2 后提交一个 CWE 单项可获得且仅获得一次 L3 分数。
- 同级另一个候选再次提交走“已解/重复”路径，不重复计分。

### 6. 更新规则说明和 E2E 证据

涉及文件：

- `memo/比赛通告-Flag提交格式.md`
- `docs/OH_CTF_ADAPTATION.md`
- `docs/e2e-contest-flow/TEST_PLAN.md`
- `docs/e2e-contest-flow/e2e-results.json`
- `docs/e2e-contest-flow/screenshots/` 中受影响或新增截图

更新：

- L3 规则明确组合 CWE 可提交整串或任一候选项。
- 删除“组合 CWE 必须顺序完全一致”的过时说明。
- E2E 增加以下断言：
  - Exact 导入界面可配置 OR 分隔符；
  - 导入组合 CWE 后单项提交成功；
  - 外题 TrackId 返回 400 且状态不变；
  - 并发报名最终只有一条 Participation 且不突破成员上限。
- 更新结构化结果和必要截图。
- Playwright trace 仅本地保存，不提交包含会话数据的文件。

## Verification

### A. 静态和构建门

1. `dotnet restore src/GZCTF.slnx`
2. `dotnet build src/GZCTF.slnx -c Release --no-restore`
3. `pnpm --dir src/GZCTF/ClientApp check`
4. `pnpm --dir src/GZCTF/ClientApp build`
5. `git diff --check`

### B. 单元测试

1. 直接运行 `dotnet test src/GZCTF.Test/GZCTF.Test.csproj -c Release`。
2. 确认日志包含实际执行的测试数量，不能只以退出码 0 判定成功。
3. 定向运行 `ScoringServiceTests` 和 `FlagImportModelsTests`。

### C. 本地模式集成测试

1. 通过 Testcontainers 启动隔离 PostgreSQL。
2. 运行全部：

   ```bash
   GZCTF_INTEGRATION_TEST_MODE=local \
   dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Release
   ```

3. 再单独运行 `GameJoinTests`、`MultiLevelFlagFlowTests`、`MultiLevelFlagImportTests` 和数据库约束测试，保留详细日志。
4. 测试结束后确认没有残留 Testcontainers。

### D. 云模式集成测试

1. 使用隔离 K3s、MinIO、PostgreSQL Testcontainers。
2. 运行全部：

   ```bash
   GZCTF_INTEGRATION_TEST_MODE=cloud \
   dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Release
   ```

3. 确认 Track、报名、导入改动没有破坏云存储或容器模式。

### E. 数据库迁移验证

1. 空数据库从零应用全部迁移，确认成功。
2. 数据库停在上一迁移且无重复数据时，应用新迁移并验证唯一索引存在。
3. 在隔离数据库构造重复 `(TeamId, GameId)` 后应用新迁移，确认迁移明确中止且不修改业务数据。
4. 执行并发报名压力测试，确认数据库始终只有一条 Participation。

### F. Chrome 竞赛流程

在隔离比赛环境中按管理员、选手、监控三种视角验证：

1. 管理员通过比赛级导入页面配置 Exact OR 分隔符并导入组合 CWE。
2. 选手完成正常 L1/L2，使用一个 CWE 候选完成 L3。
3. 直接 API 使用外题 TrackId，确认 400 且提交历史、分数、冷却不变。
4. 两名队员并发报名，确认前端状态和后台 Participation/成员数量一致。
5. 排行榜、提交记录、题目进度、封禁和冷却原流程回归通过。
6. 管理端导入、队伍进度和监控页面无 404、无英文 fallback、无控制台错误。

### G. 上线前门禁

1. 对正式数据库执行重复 Participation 预检 SQL，必须返回 0 行。
2. 完成数据库备份并验证可恢复。
3. 在维护窗口应用迁移；若预检异常立即停止，不自动清理数据。
4. 部署后观察：
   - 非法 Track 400 数量；
   - Participation 唯一约束冲突；
   - 报名 4xx/5xx；
   - Flag 接受率和排行榜分数变化。
5. 四层测试、迁移验证、Chrome E2E 任一失败均阻止上线。
