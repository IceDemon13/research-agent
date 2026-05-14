"""Quick diagnostic: figure out which Langfuse region your keys belong to.

Run after filling .env:

    python diag.py

It probes both US and EU Langfuse Cloud regions with your public/secret keys
and tells you which one to set in LANGFUSE_BASE_URL.
"""
from __future__ import annotations

import os
import sys

from dotenv import load_dotenv

load_dotenv(".env")


HOSTS = [
    ("US", "https://us.cloud.langfuse.com"),
    ("EU", "https://cloud.langfuse.com"),
]


def shape(value: str | None) -> str:
    if not value:
        return "MISSING"
    return f"{value[:7]}...{value[-3:]} (len={len(value)})"


def probe(host: str) -> tuple[bool, str]:
    """Return (ok, message) for hitting Langfuse's auth-check endpoint."""

    import httpx  # type: ignore

    public_key = os.environ.get("LANGFUSE_PUBLIC_KEY") or ""
    secret_key = os.environ.get("LANGFUSE_SECRET_KEY") or ""
    try:
        resp = httpx.get(
            f"{host.rstrip('/')}/api/public/projects",
            auth=(public_key, secret_key),
            timeout=15,
        )
    except Exception as exc:
        return False, f"network error: {exc!r}"
    if resp.status_code == 200:
        try:
            data = resp.json()
            names = [p.get("name") for p in data.get("data", [])]
        except Exception:
            names = []
        return True, f"200 OK; projects={names}"
    return False, f"HTTP {resp.status_code}: {resp.text[:200]}"


def main() -> int:
    print("=" * 60)
    print("Key shapes (truncated):")
    print(f"  LANGFUSE_PUBLIC_KEY : {shape(os.environ.get('LANGFUSE_PUBLIC_KEY'))}")
    print(f"  LANGFUSE_SECRET_KEY : {shape(os.environ.get('LANGFUSE_SECRET_KEY'))}")
    print(f"  LANGFUSE_BASE_URL   : {os.environ.get('LANGFUSE_BASE_URL')!r}")
    print("=" * 60)

    found: list[str] = []
    for label, host in HOSTS:
        print(f"\n>>> Trying {label} region: {host}")
        ok, msg = probe(host)
        print(f"   {'OK ' if ok else 'FAIL'}  {msg}")
        if ok:
            found.append(host)

    print("\n" + "=" * 60)
    if len(found) == 1:
        good = found[0]
        current = os.environ.get("LANGFUSE_BASE_URL")
        if current == good:
            print(f"All good. LANGFUSE_BASE_URL={good} matches your keys.")
        else:
            print("FIX: edit .env and set:")
            print()
            print(f"    LANGFUSE_BASE_URL={good}")
            print()
            print("Then re-run .\\run_demo.ps1")
        return 0
    if not found:
        print("FAIL: keys did not auth on any region.")
        print("Likely cause: typo in keys, extra quotes/whitespace in .env,")
        print("or the keys were deleted in Langfuse UI.")
        print()
        print("Open Langfuse UI -> Settings -> API Keys and either re-copy")
        print("the existing keys or click 'Create new API keys'.")
        return 1
    print(f"Weird: keys auth'd on both regions: {found}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
