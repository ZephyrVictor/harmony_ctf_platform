---
name: openharmonyctf-platformskill
description: "Use when an agent needs to participate in the OpenHarmony CTF platform through backend APIs: authenticate as a player, list visible contests for user selection, create/join teams, join a selected game, inspect challenge state, submit Level 1/2/3 answers, poll submission status, handle duplicate/cooldown/ban responses, and create or list 0day tickets."
---

# OpenHarmonyCTF-PlatformSKill

## Overview

Use this skill when acting as a contest participant through backend APIs. The platform uses login cookies for player endpoints; do not use Bearer API tokens for normal `RequireUser` contest actions.

Use the bundled client unless you have a strong reason to call the API manually:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py --base-url "$HARMONY_CTF_BASE_URL" <command>
```

The contest platform is available at `https://thuctf.tsinghua.edu.cn/harmonyctf/`, but always ask the user for the real backend base URL when it is not already provided.

This skill is intended for contestant-side operation only. It must not contain or infer challenge answers, hidden trap answers, imported dataset rows, admin endpoints, or privileged workflows. Treat every answer value as user-supplied competition work.

## Required User Interaction

Do not pick the contest silently.

1. Ask the user for the backend base URL if missing.
2. Probe `config` and public `games` when possible, then run `whoami` before any user-only operation.
3. If `whoami` or any later operation returns `401`, `Unauthorized`, or `Please log in first`, actively ask the user for username/password or an already-authenticated cookie jar. Do not merely report that login is required.
4. Log in after credentials are provided, verify `whoami`, then continue.
5. List visible contests with `games`.
6. Show the contest IDs, titles, time state, and summaries to the user.
7. Ask the user which contest to use.
8. After contest selection, inspect game detail, teams, and join requirements.
9. Ask before creating a team, accepting a team invite, joining a game, submitting an answer, or creating a 0day ticket.

Keep credentials out of transcripts when possible. Prefer `--password-env HARMONY_CTF_PASSWORD` or interactive password input over `--password`.

## Backend Client Commands

Set reusable variables:

```bash
export HARMONY_CTF_BASE_URL='https://thuctf.tsinghua.edu.cn/harmonyctf/'
export HARMONY_CTF_COOKIE_JAR="$HOME/.cache/harmony_ctf_platform/agent.cookies"
```

Check encryption/captcha:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py config
```

The client automatically handles:

- Cookie persistence via Mozilla cookie jar.
- `GET /api/config` and `apiPublicKey` encryption for password and answer fields.
- `GET /api/captcha` and HashPow captcha solving.
- Cloudflare Turnstile detection; request a browser token from the user if enabled.
- Default compact output that avoids printing raw answer-bearing fields. Use `--raw` only for debugging after confirming the response does not expose sensitive data.

Log in:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py login \
  --username "$HARMONY_CTF_USERNAME" \
  --password-env HARMONY_CTF_PASSWORD
```

Verify current user:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py whoami
```

If this returns `401 Unauthorized` or `Please log in first`, immediately ask the user for the account username and password, or ask for a cookie jar exported from an already logged-in browser/session. After credentials are supplied, run `login` and then `whoami` again before continuing.

List contests for user selection:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py games --count 50
```

Get selected contest detail:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py game <gameId>
```

List current user's teams:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py teams
```

Create or join a team only after user confirmation:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py create-team --name '<team name>' --bio '<optional bio>'
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py accept-team --invite-code '<teamName:teamId:token>'
```

Check and join a contest:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py check-game <gameId>
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py join-game <gameId> \
  --team-id <teamId> \
  --division-id <divisionId-if-needed> \
  --invite-code '<invite-if-needed>' \
  --accept-commitment
```

Use `--accept-commitment` only after the user explicitly confirms joining. The backend request body is:

```json
{
  "teamId": 1,
  "divisionId": null,
  "inviteCode": null,
  "commitmentAccepted": true
}
```

## Challenge Discovery

After the selected contest is joined and accepted, list playable challenges:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py details <gameId>
```

Important fields:

- `challenges[]`: compact list of challenge `id`, `title`, `category`, `score`, `solved`, and `repository`.
- `zeroDayNdayScoreRequirement`: Nday score required before 0day submission; `0` means disabled.
- `ndayScore`: current positive Nday score.
- `canSubmitZeroDay`: whether 0day ticket creation is currently allowed.
- `cooldownMinutes`: per-game challenge cooldown duration.

Inspect a challenge before every answer submission:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py challenge <gameId> <challengeId>
```

Important fields:

- `currentLevel`: frontend hint for the next level of the most recent active track. It is not a global submission constraint.
- `activeTrackId`: most recent active track. Use it only when continuing that same vulnerability chain.
- `tracks[].trackId`: track IDs to use for Level 2+.
- `tracks[].levels[].status`: `Unsolved`, `Solved`, `Banned`, or `Locked`.
- `cooldown.cooldownUntil`: pause submissions if present and in the future.
- `challengeLock.isLocked`: stop until `unlocksAt` if locked.

The compact `challenge` command intentionally returns only safe challenge-state summaries. It does not print raw visible flag objects, trap trigger answers, or any answer-bearing fields by default.

## Answer Submission

All Nday levels use:

```http
POST /api/game/{gameId}/challenges/{challengeId}
```

Backend body:

```json
{
  "flag": "answer",
  "level": 1,
  "trackId": null
}
```

Client command:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py submit <gameId> <challengeId> \
  --level 1 \
  --answer '<answer>' \
  --poll
```

Submission rules:

- Submit Level 1 without `trackId`.
- Save the Level 1 response `trackId`.
- Submit Level 2 and Level 3 with that same `trackId`.
- To start another vulnerability chain, submit another Level 1 without `trackId`, even if `currentLevel` is 2/3 and `activeTrackId` is set for a previous partial chain.
- Poll `/status/{submitId}` after each submit; `--poll` does this once.

### Starting Another Vulnerability Before Finishing The Current One

The platform supports multiple vulnerability chains in one challenge. A partially completed chain does not block starting a new one.

If the user says they want to submit a new vulnerability, do **not** continue with the current `activeTrackId`. Run a fresh Level 1 submission and omit `--track-id`:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py submit <gameId> <challengeId> \
  --level 1 \
  --answer '<new-vulnerability-L1-answer>' \
  --poll
```

When that Level 1 is accepted, store the returned `trackId` as a new chain. Keep the older `trackId` too, so the user can later resume the previous chain's Level 2/3 by explicitly choosing it.

Do not infer from `currentLevel: 2` or `activeTrackId` that Level 1 submissions are forbidden. They only describe the frontend's currently active chain. The real backend rule is:

- Level 1 starts/selects a chain and should be submitted without `trackId`.
- Level 2/3 continue a specific chain and require the chain's `trackId`.
- A new Level 1 can still be accepted while another chain is only partially solved, unless the whole challenge is under cooldown or challenge lock.

Level 2/3 example:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py submit <gameId> <challengeId> \
  --level 2 \
  --track-id '<trackId-from-level-1>' \
  --answer '<answer>' \
  --poll
```

Successful Level 1 responses include:

```json
{
  "id": 1,
  "message": "Accepted",
  "trackId": "498d686c-5ba3-42a3-b3fa-736ef3429cef",
  "level": 1,
  "activeTrackId": "498d686c-5ba3-42a3-b3fa-736ef3429cef",
  "isTrackComplete": false
}
```

Status values:

- `Accepted`: correct answer.
- `WrongAnswer`: wrong answer; `CheatDetected` is mapped to this by the status endpoint.
- `FlagSubmitted`: already submitted; do not retry the same level.

## Duplicate, Ban, And Cooldown Handling

If response `message` says the level was already submitted, or status is `FlagSubmitted`, do not retry the same level. Query challenge state and move to the next `Unsolved` level for that track.

Trap bans may be enabled by admins. Do not try to infer or enumerate trap answers.

Ban indicators:

- `bannedTrackId` is not null.
- `bannedLevel` is set.
- `challengeLock.bannedTrackId` or `challengeLock.bannedLevel` appears in challenge state.
- Message contains a track/level ban warning.

When a ban appears, stop submitting that track/level and ask the user before trying any alternate path.

Cooldown behavior:

- Wrong-answer limit is `track count * 5`.
- If `cooldown.cooldownUntil` is present and in the future, pause that challenge until the timestamp.
- If `challengeLocked` is true, pause until `unlocksAt`.

## 0day Tickets

Before creating a 0day ticket, run:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py details <gameId>
```

Only create a ticket if `canSubmitZeroDay` is true. If false, explain the current `ndayScore` and `zeroDayNdayScoreRequirement` to the user.

Create a ticket only after user confirmation:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py zeroday-create \
  --game-id <gameId> \
  --challenge-id <challengeId-if-known> \
  --title '<short title>' \
  --description '<required description>' \
  --severity High \
  --module '<module/submodule>' \
  --file-path '<source file>' \
  --line-number '<line-or-range>' \
  --commit-hash '<commit-hash>' \
  --cwe '<cwe-id>' \
  --poc '<proof or reproduction notes>'
```

Severity must be one of `None`, `Low`, `Medium`, `High`, or `Critical`.

List current user's tickets:

```bash
python3 docs/to_gamer/skills/openharmonyctf-platformskill/scripts/contest_api.py zeroday-mine --game-id <gameId>
```

## Manual API Reference

Use these endpoints if the script is unavailable:

- `GET /api/config`
- `GET /api/captcha`
- `GET /api/captcha/powchallenge`
- `POST /api/account/login`
- `GET /api/account/profile`
- `GET /api/game?count=50&skip=0`
- `GET /api/game/{gameId}`
- `GET /api/team`
- `POST /api/team`
- `POST /api/team/accept`
- `GET /api/game/{gameId}/check`
- `POST /api/game/{gameId}`
- `GET /api/game/{gameId}/details`
- `GET /api/game/{gameId}/challenges/{challengeId}`
- `POST /api/game/{gameId}/challenges/{challengeId}`
- `GET /api/game/{gameId}/challenges/{challengeId}/status/{submitId}`
- `POST /api/ext/zeroday-tickets`
- `GET /api/ext/zeroday-tickets/mine?gameId={gameId}`

## Agent Checklist

1. Confirm backend base URL.
2. Probe `config`, `games`, and `whoami`.
3. If not logged in, ask the user for username/password or a valid cookie jar, then log in and verify `whoami`.
4. List contests and ask the user to choose one.
5. Inspect selected contest, team list, join checks, divisions, and invite requirements.
6. Ask before team creation/invite acceptance/game join.
7. After joining, run `details` and show available challenges.
8. Before every answer, run `challenge` and verify level, track, cooldown, lock, and ban state.
9. Ask before submitting any answer.
10. Poll status after submission and preserve returned `trackId`.
11. Ask before creating any 0day ticket; first verify the Nday score gate.
