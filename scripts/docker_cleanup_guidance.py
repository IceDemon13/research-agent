from __future__ import annotations

import argparse
import json
import shutil
import subprocess


def _run(command: list[str]) -> dict[str, object]:
    if shutil.which(command[0]) is None:
        return {
            "command": command,
            "available": False,
            "returncode": 127,
            "stdout": "",
            "stderr": f"{command[0]} is not installed or not on PATH.",
        }
    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    return {
        "command": command,
        "available": True,
        "returncode": int(completed.returncode),
        "stdout": str(completed.stdout or "").strip(),
        "stderr": str(completed.stderr or "").strip(),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Docker-level cleanup guidance for research-agent")
    parser.add_argument("--apply", action="store_true", help="Run docker prune commands.")
    parser.add_argument("--include-system-prune", action="store_true", help="Allow docker system prune -a.")
    parser.add_argument("--include-volume-prune", action="store_true", help="Allow docker volume prune.")
    parser.add_argument("--json", action="store_true", help="Print JSON output.")
    args = parser.parse_args()

    payload = {
        "mode": "apply" if args.apply else "report",
        "scope": "docker-level-cleanup-not-repo-cleanup",
        "report": {
            "docker_system_df": _run(["docker", "system", "df"]),
            "docker_volume_ls": _run(["docker", "volume", "ls"]),
        },
        "apply_results": [],
        "warnings": [
            "This script operates on Docker images/containers/volumes, not on repo files.",
            "Destructive prune commands are never run unless --apply is passed with explicit prune flags.",
        ],
    }

    if args.apply:
        if args.include_system_prune:
            payload["apply_results"].append(_run(["docker", "system", "prune", "-a", "-f"]))
        if args.include_volume_prune:
            payload["apply_results"].append(_run(["docker", "volume", "prune", "-f"]))
        if not args.include_system_prune and not args.include_volume_prune:
            payload["warnings"].append("No prune action was selected. Pass --include-system-prune and/or --include-volume-prune.")

    if args.json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print("Docker Cleanup Guidance")
        print(f"mode: {payload['mode']}")
        print("scope: docker-level-cleanup-not-repo-cleanup")
        print("")
        for name, item in dict(payload["report"]).items():
            print(name)
            print(f"command: {' '.join(list(item.get('command', []) or []))}")
            print(f"returncode: {item.get('returncode')}")
            if item.get("stdout"):
                print(item["stdout"])
            if item.get("stderr"):
                print(item["stderr"])
            print("")
        if payload["apply_results"]:
            print("apply_results")
            for item in list(payload["apply_results"]):
                print(f"command: {' '.join(list(item.get('command', []) or []))}")
                print(f"returncode: {item.get('returncode')}")
                if item.get("stdout"):
                    print(item["stdout"])
                if item.get("stderr"):
                    print(item["stderr"])
                print("")
        if payload["warnings"]:
            print("warnings")
            for warning in list(payload["warnings"]):
                print(f"- {warning}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
