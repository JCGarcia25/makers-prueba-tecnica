from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen


def request_json(url: str, method: str = "GET", payload: dict | None = None, token: str | None = None) -> dict:
    body = json.dumps(payload).encode() if payload is not None else None
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    try:
        with urlopen(Request(url, data=body, headers=headers, method=method), timeout=20) as response:
            return json.load(response)
    except HTTPError as error:
        detail = error.read().decode(errors="replace")
        raise RuntimeError(f"La API respondió {error.code}: {detail}") from error


def main() -> None:
    parser = argparse.ArgumentParser(description="Extrae un snapshot anonimizado desde la API.")
    parser.add_argument("--api", default="http://localhost:5080")
    parser.add_argument("--email", default="admin@test.com")
    parser.add_argument("--password", required=True)
    parser.add_argument("--output", type=Path, default=Path("analytics/data/loans.json"))
    args = parser.parse_args()
    session = request_json(f"{args.api}/api/auth/login", "POST", {"email": args.email, "password": args.password})
    snapshot = request_json(f"{args.api}/api/analytics/loans", token=session["accessToken"])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(snapshot, indent=2), encoding="utf-8")
    print(f"Snapshot guardado en {args.output.resolve()}")


if __name__ == "__main__":
    main()
