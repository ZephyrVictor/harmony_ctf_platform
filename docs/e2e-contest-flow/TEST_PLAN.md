# Contest Flow Chrome E2E Test Plan

## Scope

This plan verifies the current OpenHarmony CTF contest flow from both admin and player perspectives in Chrome:

- Admin game/challenge management pages.
- Player game entry, challenge list, challenge modal, submissions page.
- Multi-level Nday answer flow: L1 source location, L2 commit hash, L3 CWE type.
- Trap behavior for bad commit answers, especially `hash(0)`.
- Admin trap enable/disable UI for per-level ban answers.
- Cooldown threshold and duration.
- Admin ban and monitor pages after trap/cooldown activity.

## Environment

- Frontend: `http://127.0.0.1:63000`
- Backend: `http://127.0.0.1:8080`
- Browser: `/usr/bin/google-chrome`, driven by Playwright.
- Database: isolated PostgreSQL on port `15432`, database `gzctf_e2e`.
- API encryption: disabled for the temporary E2E environment.
- Final run ID: `2026-07-14T12-06-43-089Z`

Evidence files:

- Results JSON: `docs/e2e-contest-flow/e2e-results.json`
- Gate disabled API evidence: `docs/e2e-contest-flow/logs/zeroday-gate-disabled.json`
- Playwright traces were generated locally for `2026-07-14T12-06-43-089Z` but are intentionally not committed because they may contain test-session request data.

## Test Data

The E2E run creates a fresh game, team, challenge, and two vulnerability tracks:

- Game ID: `2`
- Challenge ID: `2`
- Team ID: `2`
- Normal track: `e1580fbd-3e66-4dcf-b3d1-771d3b4a7e4c`
- Trap track: `c3761d6a-7545-4c62-93a1-00152cb492a5`
- Cooldown setting: `5` minutes
- Track count: `2`
- Expected wrong-answer limit: `2 * 5 = 10`

Normal track answers:

- L1: `ecmascript/builtins/builtins_arraybuffer.cpp:550-558`
- L2: `af0534741d9990427700aa4c43f2080b42c0a93f`
- L3: `CWE-476`

Trap track answers:

- L1: `ecmascript/builtins/builtins.cpp:286、ecmascript/base/json_stringifier_optimized.cpp:856-864`
- L2 valid answer: `b9f1d78f918cfe3559c93f0b7a1ad6e05d90c111`
- L2 banned answers: `0`, `hash(0)`, `0000000000000000000000000000000000000000`
- L3: `CWE-787`

## Screenshot Checklist

| Step | Screenshot | Purpose |
| --- | --- | --- |
| 1 | `screenshots/01-admin-challenges-list.png` | Admin can view the game challenge list and imported/created challenge. |
| 2 | `screenshots/02-admin-import-flags-page.png` | Admin game-level flag import UI is reachable. |
| 3 | `screenshots/03-admin-bans-empty.png` | Ban management initially has no trap bans for this run. |
| 4 | `screenshots/18-admin-zeroday-gate-config.png` | Admin game info page exposes the localized 0day Nday score gate, defaulting to `200`, with `0` documented as disabled. |
| 5 | `screenshots/15-admin-level-trap-disabled.png` | Admin Level editor shows Trap ban disabled by default. |
| 6 | `screenshots/14-admin-level-trap-enabled.png` | Admin can explicitly enable Trap ban and fill recommended values. |
| 7 | `screenshots/16-admin-level-trap-restored.png` | Admin can restore recommended Trap answers including `hash(0)`. |
| 8 | `screenshots/04-player-game-home.png` | Player can open the contest home page after login. |
| 9 | `screenshots/05-player-challenge-list.png` | Player can see the enabled challenge in the challenge list. |
| 10 | `screenshots/06-player-challenge-modal-initial.png` | Player challenge modal shows multi-level progress and level labels. |
| 11 | `screenshots/07-player-after-normal-track.png` | Normal L1/L2/L3 chain is completed and visible in UI. |
| 12 | `screenshots/08-player-after-hash0-ban.png` | `hash(0)` trap ban is visible from player perspective. |
| 13 | `screenshots/09-player-cooldown-notice.png` | Cooldown notice is visible after reaching wrong-answer threshold. |
| 14 | `screenshots/10-player-submissions.png` | Player submissions page records accepted, wrong, banned, and cooldown submissions. |
| 15 | `screenshots/13-player-zeroday-info.png` | Player 0day information page is reachable and is not a 404. |
| 16 | `screenshots/17-player-zeroday-gate.png` | Player 0day ticket submission is gated before the Nday score threshold. |
| 17 | `screenshots/11-admin-bans-after-trap.png` | Admin ban management shows the trap ban. |
| 18 | `screenshots/12-monitor-submissions.png` | Admin monitor submissions page shows contest submission activity. |

## Assertion Checklist

The final run made 20 submission/status assertions:

1. `ecmascript/builtins/builtins_arraybuffer.cpp:552` accepted as L1 subrange match.
2. `af0534741d9990427700aa4c43f2080b42c0a93f` accepted as L2 commit hash.
3. `ecmascript/builtins/builtins.cpp:286` accepted as another track's L1 after the first track reached L2.
4. Re-submitting the already solved L1 returns `FlagSubmitted` with `该级别已提交过，请继续提交下一 Level`.
5. `CWE-476` accepted as L3 and completes the normal track.
6. `deadbeefdeadbeefdeadbeefdeadbeefdeadbeef` rejected as wrong L2 commit hash.
7. `hash(0)` rejected and creates a ban on trap track L2 with `该 Track 的当前 Level 已被封禁，请联系管理员`.
8. Submitting trap-track L3 after L2 is banned returns `WrongAnswer` with `请先提交上一 Level`, not `FlagSubmitted`.
9. Trap-track L3 remains `Locked` and `currentLevel` remains L2 after L2 is banned.
10. `0` after the ban is rejected with the same current Track/Level ban message.
11. Additional wrong L1 submissions increment the shared challenge wrong count.
12. Wrong-answer limit is `10`, matching `track count * 5`.
13. The threshold submission resets `wrongCount` to `0` and sets `cooldownUntil`.
14. Cooldown duration is approximately 5 minutes.
15. Submission during cooldown returns `题目冷却中，剩 04:59`.
16. Every visited admin/player page is asserted not to be the 404 page before screenshots are saved.
17. Chinese challenge modal states are asserted not to contain the known English fallback strings from the prior regression.
18. The player 0day information route `/games/{id}/zeroday-info` is asserted not to be 404.
19. Scoreboard scoring is level-only: accepted levels sum to 15 in this run, while the challenge solve marker has `score: 0`.
20. R19 0day gate rejects 0day ticket creation when the team has only 15 Nday points and the gate is 200.

The admin UI portion also asserts:

- Game info exposes `0day 提交 Nday 分数门槛`, defaults to `200`, and documents `0` as disabling the gate.
- Level editor Trap controls are localized and are verified through Chrome, not just backend API calls.
- Trap is disabled by default for newly created/imported levels.
- Trap can be explicitly enabled by setting `banOnAnswers`.
- Recommended Trap answers can be restored and include `hash(0)`.

The acceptance basis is Chrome frontend E2E. Direct API checks are kept only as supporting assertions for submission status, backend enforcement, and the `0` gate-disable edge case.

The player 0day portion also asserts:

- The 0day ticket button is disabled before the configured Nday score gate is met.
- The backend rejects direct 0day ticket creation below the gate.
- A targeted API check sets `ZeroDayNdayScoreRequirement=0`, verifies direct ticket creation returns `201`, then restores the gate to `200`.

Detailed assertion data is in `e2e-results.json`.

## Bugs Found And Fixed

During the first full E2E run, the trap track L1 answer:

```text
ecmascript/builtins/builtins.cpp:286
```

was incorrectly rejected when the stored answer contained multiple complete source locations:

```text
ecmascript/builtins/builtins.cpp:286、ecmascript/base/json_stringifier_optimized.cpp:856-864
```

Root cause: `SourceLocation` matching only parsed OR separators inside the line expression for a single path, not multiple complete `path:line` candidates.

Fix:

- `ScoringService` now parses `SourceLocation` answers into one or more full source-location candidates.
- Existing same-path line OR behavior, such as `path.cpp:281、286`, is preserved.
- Unit coverage was added for complete source-location OR answers.

Verification:

- `dotnet test src/GZCTF.Test/GZCTF.Test.csproj --filter ScoringServiceTests -m:1` passed: 32 tests.
- `dotnet build src/GZCTF/GZCTF.csproj -m:1` passed.
- Full Chrome E2E rerun passed with `bugs: []`.

During the scoring-focused rerun, another rule mismatch was found:

- Symptom: challenge solve marker was already `score: 0`, but wrong answers still fell back to global `Scoring.DefaultPenaltyScore = 2`, causing the scoreboard total to drift below the accepted level-score sum.
- Fix: wrong-answer penalty now respects the level config. If a level has `PenaltyScore = 0`, it remains no-penalty; challenge/global default penalty is only used when no level config exists.
- Verification: unit coverage was added for level-zero penalty, and the final E2E run verified level-only scoring with `scoreboardItem.score = 15` and `solvedChallenges[0].score = 0`.

During the trap regression rerun, a solved-state mismatch was found:

- Symptom: after L2 `hash(0)` banned a track, submitting the same track's L3 CWE could return the duplicate/`FlagSubmitted` path even though L3 was not solved.
- Fix: matched-but-blocked answers now distinguish already-solved, banned, and previous-level-missing states. Previous-level-missing returns `WrongAnswer` with `请先提交上一 Level`. `currentLevel` no longer skips a banned L2 to L3.
- Verification: unit coverage was added for the blocked-match result and current-level resolution; final E2E verifies L3 remains locked after L2 ban.

During the production-readiness review, two concurrency hardening changes were made:

- Trap ban creation is idempotent on the unique `(ParticipationId, ChallengeId, TrackId, Level)` index.
- Cooldown wrong-count mutation now happens under a deterministic PostgreSQL transaction advisory lock, with the read/modify/write performed inside the lock.

During the R19 implementation, a configurable 0day gate was added:

- Game configuration now includes `ZeroDayNdayScoreRequirement` (default 200, 0 disables the gate).
- `POST /api/ext/zeroday-tickets` rejects teams below the gate using Nday Level score only.
- Player UI disables the 0day ticket submit button and shows current/required Nday score.

## Final Quality Gates

The final pass also ran repository-level quality gates after the focused Chrome E2E:

| Gate | Command / Evidence | Result |
| --- | --- | --- |
| Backend tests | `TMPDIR=/dev/shm/gz-e2e DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false /data00/home/fengjingwei.369/.dotnet/dotnet test src/GZCTF.Test/GZCTF.Test.csproj -m:1 --no-restore` | Passed: 212 tests |
| Frontend type check | `TMPDIR=/dev/shm/gz-e2e corepack pnpm check` | Passed |
| Frontend production build | `TMPDIR=/dev/shm/gz-e2e VITE_BACKEND_URL=http://127.0.0.1:8080 corepack pnpm exec vite build` | Passed |
| Locale/result JSON parse | `zh-CN/en-US admin.json`, `zh-CN/en-US challenge.json`, `e2e-results.json` | Passed |
| E2E evidence integrity | `bugs=0`, `screenshots=18`, `traces=2` generated locally, `apiChecks=20`, no missing committed screenshot/result files | Passed |
| Formatting | `git diff --check` | Passed |
| Service availability | `ss -ltnp | grep -E ':(8080|63000|15432)\b'` | Frontend/backend kept running for manual validation |

## Final Result

For this isolated E2E run, the critical contest flow passed:

- Admin pages load and show the relevant challenge/ban/monitor state.
- Player pages load and show the relevant game/challenge/submission state.
- Normal L1/L2/L3 solving works.
- Scoreboard points come only from level submissions; challenge-level solve marker score is 0.
- Wrong answers for imported/default levels with penalty 0 do not deduct points.
- Wrong commit hash is rejected.
- `hash(0)` trap bans the intended track level.
- L3 after a banned L2 is blocked as previous-level-missing instead of marked as already submitted.
- Admin can disable and restore per-level Trap answers through localized UI.
- Banned follow-up submission is rejected.
- Cooldown threshold is based on `track count * 5`.
- Cooldown duration is approximately 5 minutes and blocks further submissions.
- Chinese UI assertions pass for the challenge modal, ban notice, cooldown notice, and refreshed challenge views.
- No screenshot is accepted from a 404 page.
- The 0day information entry `/games/{id}/zeroday-info` is reachable and covered by screenshot evidence.
- The 0day ticket entry is gated by configurable Nday score and rejects direct API submissions below the gate.

## Integrity Fix Run: Issues 1, 2, and 6

Run ID: `2026-07-28-integrity-1-2-6`

This isolated Chrome run verifies the cross-challenge Track boundary, concurrent game registration, and Exact OR matching:

- Both game-level and challenge-level import pages expose `OR 分隔符` for Exact L2/L3 mappings after a real CSV preview.
- Two concurrent registration requests produce one accepted Participation only (`200`, `400`), with one UserParticipation.
- A Track from another challenge returns `400`; no Submission, SubmissionRecord, or cooldown state is created.
- `CWE-787` is accepted against `CWE-125、CWE-787`; submitting `CWE-125` afterward is idempotent and does not score twice.
- `hash(0)` still creates the intended permanent `(Track, Level)` ban.
- Ten concurrent unique wrong answers trigger one five-minute cooldown with `WrongCount=0`; a follow-up request is blocked with the remaining time.
- The scoreboard remains level-only at 15 points, and player/admin submission and ban views show the resulting activity.

New evidence:

| Screenshot | Assertion |
| --- | --- |
| `screenshots/19-admin-game-import-exact-or.png` | Game import Exact mappings expose OR separators. |
| `screenshots/20-admin-challenge-import-exact-or.png` | Challenge import Exact mappings expose OR separators. |
| `screenshots/21-player-combo-cwe-single-accepted.png` | A single CWE candidate completes the combination level. |
| `screenshots/22-player-trap-ban-regression.png` | Trap ban remains visible with `hash(0)`. |
| `screenshots/23-player-concurrent-cooldown-fixed.png` | Concurrent threshold produces an active cooldown notice. |
| `screenshots/24-player-scoreboard-level-only.png` | Player scoreboard shows 15 level points. |
| `screenshots/25-player-submissions-integrity.png` | Player submission history is reachable. |
| `screenshots/26-admin-monitor-integrity.png` | Monitor lists accepted, duplicate, trap, wrong, and cooldown activity. |
| `screenshots/27-admin-ban-integrity.png` | Admin ban page lists the automatic Track-level ban. |

The run exposed and fixed a cooldown race: requests loaded a tracked cooldown entity before waiting for the advisory lock, so EF identity resolution could overwrite newer counts. The cooldown lock now covers the check and mutation in the submission transaction, pre-check reads are no-tracking, and a five-way PostgreSQL concurrency regression test enforces the exact threshold.

Final gates after that fix:

| Gate | Result |
| --- | --- |
| Unit tests | 236 passed |
| Local PostgreSQL integration | 225 passed, 1 obsolete test skipped |
| K3s/MinIO integration | 225 passed, 1 obsolete test skipped |
| Frontend strict type check and production build | Passed |
| Migration empty/clean/duplicate scenarios | Passed; duplicate upgrade aborts without data changes |
| Chrome E2E | Passed with 9 new screenshots |

Remaining production operations should still be handled separately before public launch: switch from the temporary Development/E2E environment to Production, use HTTPS and strong secrets, and review registration/captcha/team approval policy.
