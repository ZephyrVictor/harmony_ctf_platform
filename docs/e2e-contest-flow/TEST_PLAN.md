# Contest Flow Chrome E2E Test Plan

## Scope

This plan verifies the current OpenHarmony CTF contest flow from both admin and player perspectives in Chrome:

- Admin game/challenge management pages.
- Player game entry, challenge list, challenge modal, submissions page.
- Multi-level Nday answer flow: L1 source location, L2 commit hash, L3 CWE type.
- Trap behavior for bad commit answers, especially `hash(0)`.
- Cooldown threshold and duration.
- Admin ban and monitor pages after trap/cooldown activity.

## Environment

- Frontend: `http://127.0.0.1:63000`
- Backend: `http://127.0.0.1:8080`
- Browser: `/usr/bin/google-chrome`, driven by Playwright.
- Database: isolated PostgreSQL on port `15432`, database `gzctf_e2e`.
- API encryption: disabled for the temporary E2E environment.
- Final run ID: `2026-07-07T11-21-29-388Z`

Evidence files:

- Results JSON: `docs/e2e-contest-flow/e2e-results.json`
- Admin trace: `docs/e2e-contest-flow/traces/2026-07-07T11-21-29-388Z-admin.zip`
- Player trace: `docs/e2e-contest-flow/traces/2026-07-07T11-21-29-388Z-player.zip`

## Test Data

The E2E run creates a fresh game, team, challenge, and two vulnerability tracks:

- Game ID: `1`
- Challenge ID: `1`
- Team ID: `1`
- Normal track: `498d686c-5ba3-42a3-b3fa-736ef3429cef`
- Trap track: `33bfc4d4-4a2a-4cb3-b3d4-24407b6f882a`
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
| 4 | `screenshots/04-player-game-home.png` | Player can open the contest home page after login. |
| 5 | `screenshots/05-player-challenge-list.png` | Player can see the enabled challenge in the challenge list. |
| 6 | `screenshots/06-player-challenge-modal-initial.png` | Player challenge modal shows multi-level progress and level labels. |
| 7 | `screenshots/07-player-after-normal-track.png` | Normal L1/L2/L3 chain is completed and visible in UI. |
| 8 | `screenshots/08-player-after-hash0-ban.png` | `hash(0)` trap ban is visible from player perspective. |
| 9 | `screenshots/09-player-cooldown-notice.png` | Cooldown notice is visible after reaching wrong-answer threshold. |
| 10 | `screenshots/10-player-submissions.png` | Player submissions page records accepted, wrong, banned, and cooldown submissions. |
| 11 | `screenshots/13-player-zeroday-info.png` | Player 0day information page is reachable and is not a 404. |
| 12 | `screenshots/11-admin-bans-after-trap.png` | Admin ban management shows the trap ban. |
| 13 | `screenshots/12-monitor-submissions.png` | Admin monitor submissions page shows contest submission activity. |

## Assertion Checklist

The final run made 18 submission/status assertions:

1. `ecmascript/builtins/builtins_arraybuffer.cpp:552` accepted as L1 subrange match.
2. `af0534741d9990427700aa4c43f2080b42c0a93f` accepted as L2 commit hash.
3. `ecmascript/builtins/builtins.cpp:286` accepted as another track's L1 after the first track reached L2.
4. Re-submitting the already solved L1 returns `FlagSubmitted` with `该级别已提交过，请继续提交下一 Level`.
5. `CWE-476` accepted as L3 and completes the normal track.
6. `deadbeefdeadbeefdeadbeefdeadbeefdeadbeef` rejected as wrong L2 commit hash.
7. `hash(0)` rejected and creates a ban on trap track L2 with `该 Track 的当前 Level 已被封禁，请联系管理员`.
8. `0` after the ban is rejected with the same current Track/Level ban message.
9. Additional wrong L1 submissions increment the shared challenge wrong count.
10. Wrong-answer limit is `10`, matching `track count * 5`.
11. The threshold submission resets `wrongCount` to `0` and sets `cooldownUntil`.
12. Cooldown duration is approximately 5 minutes.
13. Submission during cooldown returns `题目冷却中，剩 04:59`.
14. Every visited admin/player page is asserted not to be the 404 page before screenshots are saved.
15. Chinese challenge modal states are asserted not to contain the known English fallback strings from the prior regression.
16. The player 0day information route `/games/{id}/zeroday-info` is asserted not to be 404.
17. Scoreboard scoring is level-only: accepted levels sum to 15 in this run, while the challenge solve marker has `score: 0`.
18. Wrong answers with imported/default level `PenaltyScore = 0` do not fall back to the global default penalty.

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

## Final Quality Gates

The final pass also ran repository-level quality gates after the focused Chrome E2E:

| Gate | Command / Evidence | Result |
| --- | --- | --- |
| Backend tests | `TMPDIR=/dev/shm/gz-e2e DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false /data00/home/fengjingwei.369/.dotnet/dotnet test src/GZCTF.Test/GZCTF.Test.csproj -m:1 --no-restore` | Passed: 208 tests |
| Frontend type check | `TMPDIR=/dev/shm/gz-e2e corepack pnpm check` | Passed |
| Frontend production build | `TMPDIR=/dev/shm/gz-e2e VITE_BACKEND_URL=http://127.0.0.1:8080 corepack pnpm exec vite build` | Passed |
| Locale/result JSON parse | `zh-CN/en-US admin.json`, `zh-CN/en-US challenge.json`, `e2e-results.json` | Passed |
| E2E evidence integrity | `bugs=0`, `screenshots=13`, `traces=2`, `apiChecks=18`, no missing screenshot/trace files | Passed |
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
- Banned follow-up submission is rejected.
- Cooldown threshold is based on `track count * 5`.
- Cooldown duration is approximately 5 minutes and blocks further submissions.
- Chinese UI assertions pass for the challenge modal, ban notice, cooldown notice, and refreshed challenge views.
- No screenshot is accepted from a 404 page.
- The 0day information entry `/games/{id}/zeroday-info` is reachable and covered by screenshot evidence.

Remaining non-E2E operational risks should still be handled separately before production use, especially import file size/row limits and transfer/export preservation of new level strategy fields.
