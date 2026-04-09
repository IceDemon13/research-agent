# Storage Cleanup And Retention

## What is safe to clean

The safe cleanup flow is designed for runtime-temporary data, not for permanent repos.

Safe targets:

- `artifacts/temp-workspaces/*`
- `artifacts/host-validation-runner/*`
- `artifacts/test-temp/*`
- stale `temp*` / `tmp*` directories that are clearly runtime-temporary and not registered repos
- optional old JSON artifacts in:
  - `artifacts/draft_patch_applies`
  - `artifacts/draft_patch_executions`
- optional `.gitnexus` directories inside temp/runtime copies only

## What is not cleaned

The safe cleanup flow does not delete:

- permanent repos under `repos/*`
- `artifacts/repos/*`
- registry/config/state files
- `latest.json` / `latest_apply.json` aliases
- active or locked runtime directories
- persistent repo `.gitnexus` unless you pass the explicit dangerous flag

## Why this is safe

"Safe" here means:

- cleanup is limited to runtime-temporary locations
- persistent registered repos are preserved
- dry-run is available first
- inaccessible paths are skipped instead of crashing
- cleanup is idempotent
- Docker cleanup is separated from repo cleanup

## Dry-run commands

Repo-side dry-run report:

```powershell
python .\scripts\storage_maintenance.py
```

Repo-side dry-run JSON:

```powershell
python .\scripts\storage_maintenance.py --json
```

Docker-level dry-run report:

```powershell
python .\scripts\docker_cleanup_guidance.py
```

## Apply cleanup commands

Apply safe repo cleanup with default retention:

```powershell
python .\scripts\storage_maintenance.py --apply
```

Apply repo cleanup and also remove temp/runtime `.gitnexus` caches:

```powershell
python .\scripts\storage_maintenance.py --apply --include-temp-gitnexus
```

Apply repo cleanup with custom TTL / retention:

```powershell
python .\scripts\storage_maintenance.py --apply --temp-workspace-ttl-hours 6 --host-validation-ttl-hours 24 --artifact-keep-n 20 --artifact-max-age-days 14
```

Docker cleanup is separate and destructive only with explicit flags:

```powershell
python .\scripts\docker_cleanup_guidance.py --apply --include-system-prune --include-volume-prune
```

## Risks

- Removing temp/runtime copies can delete debugging evidence for old runs.
- Removing old execution/apply artifacts can hide older audit history if you retain too little.
- Removing persistent repo `.gitnexus` is intentionally not done by default because it can force expensive reindex work later.
- Docker prune can remove unrelated cached images/volumes outside this repo workflow.
