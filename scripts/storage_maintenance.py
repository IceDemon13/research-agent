from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from services.storage_maintenance_service import StorageMaintenanceService


def _print_human_report(payload: dict) -> None:
    print("Storage Maintenance Report")
    print(f"generated_at: {payload.get('generated_at', '')}")
    print(f"project_root: {payload.get('project_root', '')}")
    print("")
    print("Targets")
    for label, item in dict(payload.get("targets", {}) or {}).items():
        print(f"- {label}: {item.get('human', '0 B')} ({item.get('bytes', 0)} bytes)")
    print("")
    print("Top Consumers")
    for item in list(payload.get("top_consumers", []) or [])[:20]:
        print(f"- {item.get('name', item.get('path', ''))}: {item.get('human', '0 B')}")
    print("")
    print("Cleanup Candidates")
    candidates = dict(payload.get("cleanup_candidates", {}) or {})
    for name in ("temp_workspaces", "host_validation_runner", "test_temp"):
        item = dict(candidates.get(name, {}) or {})
        print(
            f"- {name}: {len(list(item.get('expired_directories', []) or []))} expired, "
            f"{len(list(item.get('skipped_locked', []) or []))} locked"
        )
    stale_temp = dict(candidates.get("stale_temp_named_dirs", {}) or {})
    print(f"- stale_temp_named_dirs: {len(list(stale_temp.get('expired_directories', []) or []))} expired")
    gitnexus = dict(candidates.get("temp_gitnexus_dirs", {}) or {})
    print(f"- temp_gitnexus_dirs: {len(list(gitnexus.get('directories', []) or []))} selected")
    print("")
    print("Artifact Retention")
    retention = dict(payload.get("artifact_retention", {}) or {})
    for name, item in retention.items():
        print(f"- {name}: {len(list(dict(item or {}).get('deletable_files', []) or []))} deletable")


def main() -> int:
    parser = argparse.ArgumentParser(description="Safe repo-side storage maintenance")
    parser.add_argument("--apply", action="store_true", help="Actually remove safe cleanup targets.")
    parser.add_argument("--json", action="store_true", help="Print JSON instead of a human summary.")
    parser.add_argument("--temp-workspace-ttl-hours", type=int, default=6)
    parser.add_argument("--host-validation-ttl-hours", type=int, default=24)
    parser.add_argument("--test-temp-ttl-hours", type=int, default=24)
    parser.add_argument("--artifact-keep-n", type=int, default=20)
    parser.add_argument("--artifact-max-age-days", type=int, default=14)
    parser.add_argument(
        "--include-temp-gitnexus",
        action="store_true",
        help="Also remove .gitnexus directories inside temp/runtime copies.",
    )
    parser.add_argument(
        "--include-persistent-repo-gitnexus",
        action="store_true",
        help="Allow selecting .gitnexus inside persistent repos too. Use with extreme caution.",
    )
    args = parser.parse_args()

    service = StorageMaintenanceService(project_root=PROJECT_ROOT)
    if args.apply:
        payload = service.apply_cleanup(
            temp_workspace_ttl_hours=args.temp_workspace_ttl_hours,
            host_validation_ttl_hours=args.host_validation_ttl_hours,
            test_temp_ttl_hours=args.test_temp_ttl_hours,
            artifact_keep_n=args.artifact_keep_n,
            artifact_max_age_days=args.artifact_max_age_days,
            include_temp_gitnexus=bool(args.include_temp_gitnexus),
            include_persistent_repo_gitnexus=bool(args.include_persistent_repo_gitnexus),
        )
    else:
        payload = service.build_report(
            temp_workspace_ttl_hours=args.temp_workspace_ttl_hours,
            host_validation_ttl_hours=args.host_validation_ttl_hours,
            test_temp_ttl_hours=args.test_temp_ttl_hours,
            artifact_keep_n=args.artifact_keep_n,
            artifact_max_age_days=args.artifact_max_age_days,
            include_temp_gitnexus=bool(args.include_temp_gitnexus),
            include_persistent_repo_gitnexus=bool(args.include_persistent_repo_gitnexus),
        )
    if args.json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        if args.apply:
            print("Storage Maintenance Apply")
            print(f"generated_at: {payload.get('generated_at', '')}")
            print(f"removed: {len(list(payload.get('removed', []) or []))}")
            print(f"skipped: {len(list(payload.get('skipped', []) or []))}")
            print("")
            for item in list(payload.get("removed", []) or []):
                print(f"- removed {item.get('path', '')} [{item.get('reason', '')}] {item.get('human', '0 B')}")
            for item in list(payload.get("skipped", []) or []):
                print(f"- skipped {item.get('path', '')} [{item.get('reason', '')}] {item.get('error', '')}")
            print("")
            _print_human_report(dict(payload.get("report", {}) or {}))
        else:
            _print_human_report(payload)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
