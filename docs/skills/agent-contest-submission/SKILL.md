---
name: agent-contest-submission
description: Use when an agent needs to authenticate to the OpenHarmony CTF platform and submit contest answers through the backend API, including player login, cookie handling, challenge state lookup, L1/L2/L3 submission payloads, submission status polling, duplicate solved-level handling, hash(0)/0 trap bans, and cooldown handling.
---

# Agent Contest Submission

## Overview

Use this skill to submit OpenHarmony CTF challenge answers as an automated player. The platform uses login cookies for player submissions; do not use Bearer API tokens for normal `RequireUser` challenge submission endpoints.

## Backend Base

Call the backend API directly:

```text
http://10.37.237.106:8080
```

The frontend preview may be available at `http://10.37.237.106:63000/`, but agents should submit to backend port `8080`.

## Authentication Workflow

1. Log in with `POST /api/account/login`.
2. Save the returned `Set-Cookie` header.
3. Send the cookie on every later request.
4. Treat the important cookie as `HarmonyCTF_Token`.

Login request:

```bash
curl -i -c cookies.txt \
  -X POST 'http://10.37.237.106:8080/api/account/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "userName": "player_112129",
    "password": "E2ePassw0rd!"
  }'
```

Expected login response:

- HTTP `200`
- no JSON body
- `Set-Cookie: HarmonyCTF_Token=...; httponly`

The current E2E environment has `ApiEncryption=false`, so `password` and `flag` are plain text. If `/api/config` returns a non-null `apiPublicKey`, encrypt sensitive fields using the frontend `encryptApiData` behavior before submitting.

## Query Challenge State

Before submitting, get the player's current state:

```bash
curl -b cookies.txt \
  'http://10.37.237.106:8080/api/game/{gameId}/challenges/{challengeId}'
```

Important fields:

- `currentLevel`: frontend hint for the next level.
- `activeTrackId`: most recent active track.
- `tracks[].trackId`: track IDs to use for L2+.
- `tracks[].levels[].status`: `Unsolved`, `Solved`, `Banned`, or `Locked`.
- `cooldown.cooldownUntil`: pause submissions if present and in the future.

## Submit Answers

All levels use one endpoint:

```http
POST /api/game/{gameId}/challenges/{challengeId}
Content-Type: application/json
Cookie: HarmonyCTF_Token=...
```

Body:

```json
{
  "flag": "answer",
  "level": 1,
  "trackId": null
}
```

Rules:

- Submit L1 without `trackId`.
- Save the L1 response `trackId`.
- Submit L2 and L3 with that `trackId`.
- To start another vulnerability chain, submit another L1 without `trackId`.

L1 example:

```bash
curl -b cookies.txt \
  -X POST 'http://10.37.237.106:8080/api/game/1/challenges/1' \
  -H 'Content-Type: application/json' \
  -d '{
    "flag": "ecmascript/builtins/builtins_arraybuffer.cpp:552",
    "level": 1
  }'
```

L2 example:

```bash
curl -b cookies.txt \
  -X POST 'http://10.37.237.106:8080/api/game/1/challenges/1' \
  -H 'Content-Type: application/json' \
  -d '{
    "flag": "af0534741d9990427700aa4c43f2080b42c0a93f",
    "level": 2,
    "trackId": "498d686c-5ba3-42a3-b3fa-736ef3429cef"
  }'
```

L3 example:

```bash
curl -b cookies.txt \
  -X POST 'http://10.37.237.106:8080/api/game/1/challenges/1' \
  -H 'Content-Type: application/json' \
  -d '{
    "flag": "CWE-476",
    "level": 3,
    "trackId": "498d686c-5ba3-42a3-b3fa-736ef3429cef"
  }'
```

Successful L1 responses include:

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

## Poll Status

After every POST, poll:

```bash
curl -b cookies.txt \
  'http://10.37.237.106:8080/api/game/{gameId}/challenges/{challengeId}/status/{submitId}'
```

Important status values:

- `Accepted`
- `WrongAnswer`
- `FlagSubmitted`

`CheatDetected` is mapped to `WrongAnswer` by this status endpoint.

## Duplicate, Trap, And Cooldown Handling

If the response message is `该级别已提交过，请继续提交下一 Level` or status is `FlagSubmitted`, do not retry the same level. Continue to the next unsolved level for that `trackId`.

Trap is disabled by default. If the administrator enables Trap ban for an L2 level, these answers can ban the current Track/Level:

```json
["0", "hash(0)", "0000000000000000000000000000000000000000"]
```

Trap response indicators:

- `bannedTrackId` is not null.
- `bannedLevel` is set.
- message contains `该 Track 的当前 Level 已被封禁，请联系管理员`.

When a ban appears, stop submitting that Track/Level.

Cooldown behavior:

- Wrong answer limit is `track count * 5`.
- If `cooldown.cooldownUntil` is present, pause the challenge until that time.
- Cooldown message example: `题目冷却中，剩 04:59`.

## Python Session Pattern

Use one HTTP session so cookies persist:

```python
import requests

base = "http://10.37.237.106:8080"
game_id = 1
challenge_id = 1

s = requests.Session()
s.post(f"{base}/api/account/login", json={
    "userName": "player_112129",
    "password": "E2ePassw0rd!",
}).raise_for_status()

r = s.post(f"{base}/api/game/{game_id}/challenges/{challenge_id}", json={
    "flag": "ecmascript/builtins/builtins_arraybuffer.cpp:552",
    "level": 1,
})
r.raise_for_status()
submit = r.json()
track_id = submit["trackId"]

status = s.get(f"{base}/api/game/{game_id}/challenges/{challenge_id}/status/{submit['id']}")
status.raise_for_status()
print(status.json())
```

## Agent Checklist

1. Log in and persist cookies.
2. Query challenge state.
3. If cooling down, wait.
4. Submit L1 without `trackId`.
5. Save `trackId` from L1.
6. Submit L2/L3 with `trackId`.
7. Poll `/status/{submitId}` after each submit.
8. Stop on bans.
9. Avoid retrying solved levels.
10. Start a different vulnerability by submitting a new L1 without `trackId`.
