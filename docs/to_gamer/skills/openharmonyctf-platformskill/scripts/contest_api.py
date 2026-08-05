#!/usr/bin/env python3
"""Backend client for the OpenHarmony CTF contest player API."""

from __future__ import annotations

import argparse
import base64
import getpass
import hashlib
import json
import os
import secrets
import stat
import sys
import time
from http.cookiejar import MozillaCookieJar
from pathlib import Path
from typing import Any
from urllib.parse import urljoin

try:
    import requests
except ImportError as exc:  # pragma: no cover - environment guard
    raise SystemExit("Missing dependency: install python package 'requests'.") from exc


DEFAULT_COOKIE_JAR = Path.home() / ".cache" / "harmony_ctf_platform" / "agent.cookies"
SENSITIVE_EXACT_KEYS = {
    "answer",
    "banonanswers",
    "bantriggeranswer",
    "flag",
    "flags",
    "flagvalue",
    "lockonanswers",
    "rawanswer",
    "triggeranswer",
}
SENSITIVE_KEY_PARTS = (
    "cookie",
    "invitecode",
    "password",
    "privatekey",
    "secret",
    "teamtoken",
    "token",
)


class ApiError(RuntimeError):
    pass


def dump(data: Any) -> None:
    print(json.dumps(redact_sensitive(data), ensure_ascii=False, indent=2, sort_keys=True))


def redact_sensitive(data: Any) -> Any:
    if isinstance(data, dict):
        redacted: dict[str, Any] = {}
        for key, value in data.items():
            normalized = key.replace("_", "").replace("-", "").lower()
            if normalized in SENSITIVE_EXACT_KEYS or any(part in normalized for part in SENSITIVE_KEY_PARTS):
                redacted[key] = "<redacted>"
            else:
                redacted[key] = redact_sensitive(value)
        return redacted

    if isinstance(data, list):
        return [redact_sensitive(item) for item in data]

    return data


def leading_zero_bits(data: bytes) -> int:
    count = 0
    for byte in data:
        if byte == 0:
            count += 8
            continue
        mask = 0x80
        while mask and (byte & mask) == 0:
            count += 1
            mask >>= 1
        break
    return count


def solve_hash_pow(challenge_hex: str, difficulty: int) -> str:
    challenge = bytes.fromhex(challenge_hex)
    random_prefix = secrets.token_bytes(4)
    nonce = secrets.randbits(32)
    while True:
        nonce_bytes = nonce.to_bytes(4, "big")
        digest = hashlib.sha256(challenge + random_prefix + nonce_bytes).digest()
        if leading_zero_bits(digest) >= difficulty:
            return (random_prefix + nonce_bytes).hex()
        nonce = (nonce + 1) & 0xFFFFFFFF


def encrypt_api_data(plain_text: str, public_key: str | None) -> str:
    if not public_key:
        return plain_text
    try:
        from cryptography.hazmat.primitives import hashes, serialization
        from cryptography.hazmat.primitives.asymmetric import x25519
        from cryptography.hazmat.primitives.ciphers.aead import AESGCM
    except ImportError as exc:  # pragma: no cover - optional dependency guard
        raise ApiError(
            "The server enables API encryption, but Python package 'cryptography' is missing. "
            "Install it or use the browser frontend to obtain a valid session cookie."
        ) from exc

    recipient = x25519.X25519PublicKey.from_public_bytes(base64.b64decode(public_key))
    ephemeral = x25519.X25519PrivateKey.generate()
    shared = ephemeral.exchange(recipient)
    digest = hashes.Hash(hashes.SHA256())
    digest.update(shared)
    key = digest.finalize()
    nonce = secrets.token_bytes(12)
    ciphertext = AESGCM(key).encrypt(nonce, plain_text.encode(), None)
    ephemeral_public = ephemeral.public_key().public_bytes(
        encoding=serialization.Encoding.Raw,
        format=serialization.PublicFormat.Raw,
    )
    return base64.b64encode(ephemeral_public + nonce + ciphertext).decode()


class ContestApi:
    def __init__(self, base_url: str, cookie_jar: Path, timeout: float = 20.0):
        self.base_url = base_url.rstrip("/") + "/"
        self.cookie_jar = cookie_jar
        self.timeout = timeout
        self.session = requests.Session()
        self.session.headers.update({"Accept": "application/json"})
        self.jar = MozillaCookieJar(str(cookie_jar))
        if cookie_jar.exists():
            self.jar.load(ignore_discard=True, ignore_expires=True)
        self.session.cookies = self.jar
        self._config: dict[str, Any] | None = None
        self._captcha: dict[str, Any] | None = None

    def save_cookies(self) -> None:
        self.cookie_jar.parent.mkdir(parents=True, exist_ok=True)
        self.jar.save(ignore_discard=True, ignore_expires=True)
        try:
            self.cookie_jar.chmod(stat.S_IRUSR | stat.S_IWUSR)
        except OSError:
            pass

    def request(self, method: str, path: str, raise_for_status: bool = True, **kwargs: Any) -> requests.Response:
        url = urljoin(self.base_url, path.lstrip("/"))
        headers = kwargs.pop("headers", {})
        kwargs["headers"] = headers
        kwargs.setdefault("timeout", self.timeout)
        response = self.session.request(method, url, **kwargs)
        self.save_cookies()
        if raise_for_status and response.status_code >= 400:
            raise ApiError(self.format_error(response))
        return response

    @staticmethod
    def read_json(response: requests.Response) -> Any:
        if not response.content:
            return None
        try:
            return response.json()
        except ValueError:
            return response.text

    @staticmethod
    def format_error(response: requests.Response) -> str:
        try:
            body = response.json()
        except ValueError:
            body = response.text
        return f"HTTP {response.status_code} {response.reason}: {json.dumps(redact_sensitive(body), ensure_ascii=False)}"

    @staticmethod
    def format_blocked_response(response: requests.Response) -> dict[str, Any]:
        try:
            body = response.json()
        except ValueError:
            return {"httpStatus": response.status_code, "reason": response.reason, "raw": response.text}
        if not isinstance(body, dict):
            return {"httpStatus": response.status_code, "reason": response.reason, "raw": body}

        return {
            "httpStatus": response.status_code,
            "scope": body.get("scope"),
            "title": body.get("title"),
            "limit": body.get("limit"),
            "used": body.get("used"),
            "remaining": body.get("remaining"),
            "resetsAt": body.get("resetsAt"),
            "status": body.get("status"),
        }

    def get_json(self, path: str, **params: Any) -> Any:
        response = self.request("GET", path, params={k: v for k, v in params.items() if v is not None})
        return self.read_json(response)

    def post_json(self, path: str, payload: Any) -> Any:
        response = self.request("POST", path, json=payload)
        return self.read_json(response)

    def config(self) -> dict[str, Any]:
        if self._config is None:
            data = self.get_json("/api/config")
            self._config = data if isinstance(data, dict) else {}
        return self._config

    def captcha(self) -> dict[str, Any]:
        if self._captcha is None:
            data = self.get_json("/api/captcha")
            self._captcha = data if isinstance(data, dict) else {}
        return self._captcha

    def api_public_key(self) -> str | None:
        value = self.config().get("apiPublicKey")
        return value if isinstance(value, str) and value else None

    def captcha_challenge(self, token: str | None = None) -> str | None:
        info = self.captcha()
        captcha_type = info.get("type") or "None"
        if captcha_type == "None":
            return None
        if captcha_type == "HashPow":
            chall = self.get_json("/api/captcha/powchallenge")
            if not isinstance(chall, dict) or not chall.get("id") or not chall.get("challenge"):
                raise ApiError(f"Invalid HashPow challenge: {chall!r}")
            nonce = solve_hash_pow(str(chall["challenge"]), int(chall.get("difficulty") or 18))
            return f"{chall['id']}:{nonce}"
        if captcha_type == "CloudflareTurnstile":
            if token:
                return token
            raise ApiError(
                "Cloudflare Turnstile captcha is enabled. Provide --captcha-token from the browser session."
            )
        raise ApiError(f"Unsupported captcha provider: {captcha_type}")

    def login(self, username: str, password: str, captcha_token: str | None = None) -> None:
        public_key = self.api_public_key()
        payload = {
            "userName": username,
            "password": encrypt_api_data(password, public_key),
        }
        challenge = self.captcha_challenge(captcha_token)
        if challenge:
            payload["challenge"] = challenge
        self.request("POST", "/api/account/login", json=payload)

    def encrypted_flag_payload(self, flag: str, level: int, track_id: str | None) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "flag": encrypt_api_data(flag, self.api_public_key()),
            "level": level,
        }
        if track_id:
            payload["trackId"] = track_id
        return payload


def compact_games(payload: Any) -> dict[str, Any]:
    games = payload.get("data", payload) if isinstance(payload, dict) else payload
    if not isinstance(games, list):
        return {"raw": payload}
    rows = []
    now = time.time()
    for game in games:
        start = game.get("start")
        end = game.get("end")
        state = "unknown"
        if isinstance(start, str) or isinstance(end, str):
            state = "listed"
        elif isinstance(start, (int, float)) and isinstance(end, (int, float)):
            if now < start:
                state = "pending"
            elif now > end:
                state = "ended"
            else:
                state = "running"
        rows.append({
            "id": game.get("id"),
            "title": game.get("title"),
            "summary": game.get("summary"),
            "state": state,
            "start": start,
            "end": end,
            "limit": game.get("limit"),
        })
    return {"games": rows, "total": payload.get("total") if isinstance(payload, dict) else len(rows)}


def compact_details(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        return {"raw": payload}
    challenges = []
    for category, items in (payload.get("challenges") or {}).items():
        for item in items or []:
            challenges.append({
                "id": item.get("id"),
                "title": item.get("title"),
                "category": category,
                "score": item.get("score"),
                "solved": item.get("solved"),
                "repository": item.get("repository"),
            })
    return {
        "rank": payload.get("rank"),
        "challengeCount": payload.get("challengeCount"),
        "zeroDayNdayScoreRequirement": payload.get("zeroDayNdayScoreRequirement"),
        "ndayScore": payload.get("ndayScore"),
        "cooldownMinutes": payload.get("cooldownMinutes"),
        "challenges": challenges,
    }


def compact_challenge(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        return {"raw": payload}

    def compact_level(level: Any) -> dict[str, Any]:
        if not isinstance(level, dict):
            return {"rawType": type(level).__name__}
        return {
            "level": level.get("level"),
            "title": level.get("title"),
            "status": level.get("status"),
            "attemptsUsed": level.get("attemptsUsed"),
            "solvedAt": level.get("solvedAt"),
            "unlocksAt": level.get("unlocksAt"),
        }

    tracks = []
    for track in payload.get("tracks") or []:
        tracks.append({
            "trackId": track.get("trackId"),
            "title": track.get("title"),
            "levels": [compact_level(level) for level in track.get("levels") or []],
        })

    visible_flags = []
    for flag in payload.get("flags") or []:
        if not isinstance(flag, dict):
            continue
        visible_flags.append({
            "level": flag.get("level"),
            "trackId": flag.get("trackId"),
            "answerType": flag.get("answerType"),
            "isSolved": flag.get("isSolved"),
        })

    challenge_lock = payload.get("challengeLock")
    if isinstance(challenge_lock, dict):
        challenge_lock = {
            "isLocked": challenge_lock.get("isLocked"),
            "unlocksAt": challenge_lock.get("unlocksAt"),
            "reason": challenge_lock.get("reason"),
            "bannedTrackId": challenge_lock.get("bannedTrackId"),
            "bannedLevel": challenge_lock.get("bannedLevel"),
        }

    submission_control = compact_submission_control(payload.get("submissionControl"))

    return {
        "id": payload.get("id"),
        "title": payload.get("title"),
        "repository": payload.get("repository"),
        "score": payload.get("score"),
        "attempts": payload.get("attempts"),
        "currentLevel": payload.get("currentLevel"),
        "activeTrackId": payload.get("activeTrackId"),
        "cooldown": payload.get("cooldown"),
        "submissionControl": submission_control,
        "challengeLock": challenge_lock,
        "tracks": tracks,
        "visibleFlags": visible_flags,
    }


def compact_submission_control(control: Any) -> Any:
    if not isinstance(control, dict):
        return control

    def quota(name: str) -> Any:
        item = control.get(name)
        if not isinstance(item, dict):
            return item
        return {
            "limit": item.get("limit"),
            "used": item.get("used"),
            "remaining": item.get("remaining"),
            "exceeded": item.get("exceeded"),
            "resetsAt": item.get("resetsAt"),
        }

    cooldown = control.get("cooldown")
    if isinstance(cooldown, dict):
        cooldown = {
            "wrongCount": cooldown.get("wrongCount"),
            "wrongAnswerLimit": cooldown.get("wrongAnswerLimit"),
            "cooldownMinutes": cooldown.get("cooldownMinutes"),
            "cooldownUntil": cooldown.get("cooldownUntil"),
        }

    return {
        "gameDailyTeam": quota("gameDailyTeam"),
        "challengeDailyTeam": quota("challengeDailyTeam"),
        "challengeTotalTeam": quota("challengeTotalTeam"),
        "cooldown": cooldown,
    }


def add_common(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--base-url", default=os.environ.get("HARMONY_CTF_BASE_URL"))
    parser.add_argument("--cookie-jar", type=Path, default=Path(os.environ.get("HARMONY_CTF_COOKIE_JAR", DEFAULT_COOKIE_JAR)))
    parser.add_argument("--timeout", type=float, default=20.0)
    parser.add_argument("--raw", action="store_true", help="Print the raw response JSON.")


def require_base_url(args: argparse.Namespace) -> None:
    if not args.base_url:
        raise SystemExit("Missing --base-url or HARMONY_CTF_BASE_URL.")


def get_password(args: argparse.Namespace) -> str:
    if args.password:
        return args.password
    env_name = args.password_env
    if env_name and os.environ.get(env_name):
        return os.environ[env_name]
    if sys.stdin.isatty():
        return getpass.getpass("Password: ")
    raise SystemExit("Missing --password, --password-env, or interactive TTY for password input.")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    add_common(parser)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("config")

    p = sub.add_parser("login")
    p.add_argument("--username", required=True)
    p.add_argument("--password")
    p.add_argument("--password-env", default="HARMONY_CTF_PASSWORD")
    p.add_argument("--captcha-token")

    sub.add_parser("whoami")
    sub.add_parser("logout")

    p = sub.add_parser("games")
    p.add_argument("--count", type=int, default=50)
    p.add_argument("--skip", type=int, default=0)
    p.add_argument("--recent", action="store_true")

    p = sub.add_parser("game")
    p.add_argument("game_id", type=int)

    sub.add_parser("teams")

    p = sub.add_parser("create-team")
    p.add_argument("--name", required=True)
    p.add_argument("--bio")

    p = sub.add_parser("accept-team")
    p.add_argument("--invite-code", required=True)

    p = sub.add_parser("check-game")
    p.add_argument("game_id", type=int)

    p = sub.add_parser("join-game")
    p.add_argument("game_id", type=int)
    p.add_argument("--team-id", type=int, required=True)
    p.add_argument("--division-id", type=int)
    p.add_argument("--invite-code")
    p.add_argument("--accept-commitment", action="store_true")

    p = sub.add_parser("details")
    p.add_argument("game_id", type=int)

    p = sub.add_parser("challenge")
    p.add_argument("game_id", type=int)
    p.add_argument("challenge_id", type=int)

    p = sub.add_parser("submit")
    p.add_argument("game_id", type=int)
    p.add_argument("challenge_id", type=int)
    p.add_argument("--level", type=int, default=1)
    p.add_argument("--track-id")
    p.add_argument("--answer", required=True)
    p.add_argument("--poll", action="store_true")
    p.add_argument(
        "--allow-track-id-on-level1",
        action="store_true",
        help="Override the safety guard and send trackId with Level 1. Normal new-vulnerability submissions should omit trackId.",
    )

    p = sub.add_parser("status")
    p.add_argument("game_id", type=int)
    p.add_argument("challenge_id", type=int)
    p.add_argument("submission_id", type=int)

    args = parser.parse_args(argv)
    require_base_url(args)
    api = ContestApi(args.base_url, args.cookie_jar, args.timeout)

    try:
        if args.command == "config":
            dump({"config": api.config(), "captcha": api.captcha()})
        elif args.command == "login":
            api.login(args.username, get_password(args), args.captcha_token)
            dump({"ok": True, "cookieJar": str(args.cookie_jar)})
        elif args.command == "whoami":
            dump(api.get_json("/api/account/profile"))
        elif args.command == "logout":
            dump(api.post_json("/api/account/logout", {}))
        elif args.command == "games":
            path = "/api/game/recent" if args.recent else "/api/game"
            payload = api.get_json(path, limit=args.count) if args.recent else api.get_json(path, count=args.count, skip=args.skip)
            dump(payload if args.raw else compact_games(payload))
        elif args.command == "game":
            dump(api.get_json(f"/api/game/{args.game_id}"))
        elif args.command == "teams":
            dump(api.get_json("/api/team"))
        elif args.command == "create-team":
            dump(api.post_json("/api/team", {"name": args.name, "bio": args.bio}))
        elif args.command == "accept-team":
            dump(api.post_json("/api/team/accept", args.invite_code))
        elif args.command == "check-game":
            dump(api.get_json(f"/api/game/{args.game_id}/check"))
        elif args.command == "join-game":
            if not args.accept_commitment:
                raise ApiError("Refusing to join without --accept-commitment after user confirmation.")
            payload = {
                "teamId": args.team_id,
                "divisionId": args.division_id,
                "inviteCode": args.invite_code,
                "commitmentAccepted": True,
            }
            dump(api.post_json(f"/api/game/{args.game_id}", payload))
        elif args.command == "details":
            payload = api.get_json(f"/api/game/{args.game_id}/details")
            dump(payload if args.raw else compact_details(payload))
        elif args.command == "challenge":
            payload = api.get_json(f"/api/game/{args.game_id}/challenges/{args.challenge_id}")
            dump(payload if args.raw else compact_challenge(payload))
        elif args.command == "submit":
            if args.level == 1 and args.track_id and not args.allow_track_id_on_level1:
                raise ApiError(
                    "Level 1 starts a new vulnerability chain and should omit --track-id. "
                    "Remove --track-id, or pass --allow-track-id-on-level1 only for explicit debugging."
                )
            payload = api.encrypted_flag_payload(args.answer, args.level, args.track_id)
            response = api.request(
                "POST",
                f"/api/game/{args.game_id}/challenges/{args.challenge_id}",
                raise_for_status=False,
                json=payload,
            )
            if response.status_code >= 400:
                dump(api.format_blocked_response(response))
                return 1
            result = api.read_json(response)
            if args.poll and isinstance(result, dict) and result.get("id") is not None:
                result["status"] = api.get_json(
                    f"/api/game/{args.game_id}/challenges/{args.challenge_id}/status/{result['id']}"
                )
            dump(result)
        elif args.command == "status":
            dump(api.get_json(f"/api/game/{args.game_id}/challenges/{args.challenge_id}/status/{args.submission_id}"))
        else:  # pragma: no cover - argparse enforces command
            raise ApiError(f"Unhandled command {args.command}")
    except (ApiError, requests.RequestException, ValueError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
