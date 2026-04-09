from __future__ import annotations

import argparse
import json
import sys
import time
import urllib.error
import urllib.request


def _request(method: str, url: str, *, payload: dict[str, object] | None = None, token: str = "", timeout: int = 30) -> dict[str, object]:
    data = None
    headers = {"Content-Type": "application/json"}
    if token:
        headers["X-Validation-Token"] = token
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(url, data=data, headers=headers, method=method.upper())
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.loads(response.read().decode("utf-8"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default="http://127.0.0.1:8092")
    parser.add_argument("--token", default="")
    parser.add_argument("--repo-id", required=True)
    parser.add_argument("--repo-path", required=True)
    parser.add_argument("--project-path", required=True)
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()

    health = _request("GET", f"{args.base_url.rstrip('/')}/health", token=args.token, timeout=10)
    print(json.dumps({"health": health}, ensure_ascii=False, indent=2))

    validate_payload = {
        "repo_id": args.repo_id,
        "repo_path": args.repo_path,
        "allowed_roots": ["/app/artifacts/temp-workspaces", "/repos"],
        "timeout_seconds": int(args.timeout),
        "commands": [
            {
                "name": "restore",
                "command": f'dotnet restore "{args.project_path}" --nologo',
                "working_dir": ".",
                "timeout_seconds": int(args.timeout),
            }
        ],
    }
    accepted = _request("POST", f"{args.base_url.rstrip('/')}/validate", payload=validate_payload, token=args.token, timeout=30)
    print(json.dumps({"accepted": accepted}, ensure_ascii=False, indent=2))
    job_id = str(accepted.get("job_id", "") or "").strip()
    if not job_id:
        print("Missing job_id in /validate response.", file=sys.stderr)
        return 1
    for _ in range(max(1, int(args.timeout))):
        time.sleep(1)
        try:
            result = _request("GET", f"{args.base_url.rstrip('/')}/validate/{job_id}", token=args.token, timeout=30)
        except urllib.error.HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            print(body, file=sys.stderr)
            return 1
        if not bool(result.get("accepted")):
            print(json.dumps({"result": result}, ensure_ascii=False, indent=2))
            return 0 if bool(result.get("ok")) else 2
    print(f"Timed out waiting for validation job {job_id}.", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
