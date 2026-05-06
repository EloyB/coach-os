#!/usr/bin/env python3
"""
Test Buffer API connection and discover connected channel profile IDs.

Usage:
    python3 scripts/test_buffer_connection.py

Reads BUFFER_ACCESS_TOKEN from ../.env and calls Buffer's /profiles.json
endpoint to confirm the token works and to list every connected channel
along with its profile_id (which we need for posting).
"""

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path


def load_env(env_path: Path) -> None:
    """Tiny .env loader — no third-party dependency."""
    if not env_path.exists():
        print(f"ERROR: .env not found at {env_path}")
        sys.exit(1)
    for raw in env_path.read_text().splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _, value = line.partition("=")
        value = value.strip().strip('"').strip("'")
        os.environ.setdefault(key.strip(), value)


def main() -> None:
    workspace = Path(__file__).resolve().parent.parent
    load_env(workspace / ".env")

    token = os.environ.get("BUFFER_ACCESS_TOKEN", "").strip()
    if not token:
        print("ERROR: BUFFER_ACCESS_TOKEN is empty in .env")
        print("       Open .env and paste your token from")
        print("       https://buffer.com/developers/apps")
        sys.exit(1)

    url = f"https://api.bufferapp.com/1/profiles.json?access_token={token}"

    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            data = json.loads(resp.read())
    except urllib.error.HTTPError as exc:
        print(f"ERROR: Buffer API returned HTTP {exc.code}")
        body = exc.read().decode(errors="replace")
        print(body)
        if exc.code == 403:
            print("\nThe token was rejected. Common causes:")
            print("  - Token copied incorrectly (check for trailing spaces)")
            print("  - Token revoked from the Buffer app dashboard")
        sys.exit(1)
    except urllib.error.URLError as exc:
        print(f"ERROR: Could not reach Buffer ({exc.reason})")
        sys.exit(1)

    if not isinstance(data, list):
        print("Unexpected response shape from Buffer:")
        print(json.dumps(data, indent=2))
        sys.exit(1)

    print(f"\nToken works. Found {len(data)} connected channel(s):\n")
    for profile in data:
        service = profile.get("service", "?")
        username = (
            profile.get("formatted_username")
            or profile.get("service_username")
            or "?"
        )
        timezone = profile.get("timezone", "?")
        print(f"  [{service:9s}] {username}")
        print(f"     profile_id : {profile.get('id')}")
        print(f"     timezone   : {timezone}")
        print()

    print("─" * 56)
    print("Next: copy these into .env (matching the right service):\n")
    for profile in data:
        service = (profile.get("service") or "").upper()
        if service in {"INSTAGRAM", "LINKEDIN"}:
            print(f"  BUFFER_PROFILE_{service}={profile.get('id')}")


if __name__ == "__main__":
    main()
