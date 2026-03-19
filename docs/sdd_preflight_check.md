# SDD Preflight Check

## Current architecture summary

### Canonical orchestration
- `main.py` is a thin executable wrapper that delegates to `console_app.main`.
- `console_app.py` builds the root agent through `agents/root_agent.py::build_agent`.
- `agents/root_agent.py` is the only implementation-mode orchestration owner for:
  - `run_spec_only_pipeline`
  - `run_spec_to_code_pipeline`
  - `run_full_change_pipeline`
  - `run_full_draft_pipeline`
  - `run_full_review_pipeline`
- `agent.py` is only a compatibility shim re-exporting `agents/root_agent.py`.

### Repo-aware execution foundation
- `services/repo_registry.py` is the canonical `repo_id -> repo metadata -> root_path` resolver.
- `services/repo_index_service.py` is the canonical persistent manifest/file-index service.
- `contracts/repo_metadata.py`, `contracts/repo_index.py`, and `contracts/repo_context_contract.py` provide typed repo-state contracts.
- `tools/repo_tools.py` is the canonical repo-aware context builder and repo read layer.
- `agents/root_agent.py` now resolves repo execution first and passes repo-aware readers/validators into the existing repo-context rules.

### Stage pipeline shape
- Spec stage: `agents/spec_agent.py`
- Code-plan stage: `agents/code_agent.py`
- Change stage: `agents/change_agent.py`
- Draft stage: `agents/draft_agent.py`
- Review stage: `agents/review_agent.py`
- Repair stage: `agents/repair_agent.py`
- Shared LLM/tool loop: `loops/react_loop.py`

### Important architectural observation
- There is still a separate research/RAG answer path in `agents/research_agent.py` -> `agents/rag_research_agent.py`, but it is a question-answering path, not a second implementation pipeline.
- That path should not become a mutation entrypoint.

## Mutation readiness assessment

### Current mutation boundaries
The implementation pipeline is currently effectively read-only with respect to registered repository source files.

Current write-capable code found in the repository architecture:

| Area | File / function | What it writes | Scope |
| --- | --- | --- | --- |
| Repo registry | `services/repo_registry.py::_save_state` | `artifacts/repos/registry.json` | Local metadata only |
| Repo indexing | `services/repo_index_service.py::_save_manifest` | `artifacts/repos/<repo_id>/repo_manifest.json` | Local metadata only |
| Repo indexing | `services/repo_index_service.py::_save_file_index` | `artifacts/repos/<repo_id>/file_index.json` | Local metadata only |
| Legacy manifest sync | `services/repo_index_service.py::_sync_legacy_manifest` | `output/repo_manifest.json` | Local metadata only, current self-repo only |
| Legacy manifest builder | `tools/repo_tools.py::build_repo_manifest` | `output/repo_manifest.json` | Local metadata only |
| Output report tool | `tools/report_tools.py::write_report` | validated file under output folder | Explicit tool write, not repo mutation |
| Task artifacts | `services/task_registry.py::write_task_artifact` | `artifacts/tasks/...` | Local task artifacts only |

### Read-only status of current implementation flows
- `agents/change_agent.py` produces `ChangeSet` objects only.
- `agents/draft_agent.py` produces `DraftSet` objects only.
- `agents/review_agent.py` and `agents/repair_agent.py` operate on in-memory contracts only.
- `agents/root_agent.py` orchestrates stage outputs through `AgentResult.metadata`; it does not apply drafts or changes to the filesystem.
- No current implementation-mode stage writes into target repo files.

### Conclusion
- Analysis/spec/change/draft/review/repair flows are currently safe from accidental repo mutation.
- The repository is not yet mutation-ready because no single reviewed apply boundary exists.

## Draft/change readiness

### Current contract usage
- `contracts/change_set.py::ChangeSet`
  - `goal`
  - `files: list[ProposedFileChange]`
  - `risks`
  - `checks`
- `contracts/proposed_file_change.py::ProposedFileChange`
  - `path`
  - `operation`
  - `why`
  - `targets`
  - `edits`
  - `checks`
- `contracts/draft_set.py::DraftSet`
  - `goal`
  - `files: list[FileDraft]`
  - `risks`
- `contracts/file_draft.py::FileDraft`
  - `path`
  - `why`
  - `content`

### How they are used today
- `agents/change_agent.py` generates a `ChangeSet` and validates file paths against repo context.
- `agents/draft_agent.py` consumes `ChangeSet`, generates `DraftSet`, and validates draft paths against repo context.
- `agents/review_agent.py` reviews `ChangeSet` and `DraftSet` alignment and checks draft safety.
- `agents/repair_agent.py` returns a repaired `DraftSet`.

### Readiness for deterministic apply
What is already sufficient:
- File mapping exists through `path`.
- High-level operation intent exists in `ProposedFileChange.operation`.
- Full-file payload exists in `FileDraft.content`.

What is still missing before safe deterministic apply:
- No base file hash / expected precondition per file.
- No explicit apply-mode contract separate from analysis artifacts.
- `FileDraft` has no explicit operation type like create/modify/delete.
- `FileDraft` has no encoding, newline, or executable-bit metadata.
- `ChangeSet.edits` are descriptive text, not structured hunks or exact edit operations.
- No atomicity policy or overwrite policy.
- No dry-run representation.
- No per-file result/trace structure.
- No explicit repo resolution snapshot attached to the mutation artifact itself.

### Assessment
- `DraftSet` is closer to apply-ready than `ChangeSet`, because it carries full file content.
- `ChangeSet` is planning-ready, not apply-ready.
- Neither contract is sufficient by itself for safe real mutation.

## Repo safety validation

### Determinism and isolation
- `services/repo_registry.py::resolve_repo` resolves registered repos deterministically from `artifacts/repos/registry.json`.
- `services/repo_index_service.py` stores persistent per-repo artifacts under `artifacts/repos/<repo_id>/`.
- `tools/repo_tools.py::_resolve_repo_runtime` resolves `repo_id` first, then derives the effective root path.
- `contracts/repo_context_contract.py` now carries both `repo_id` and `root_path`.
- Downstream agents reuse `repo_context["repo_id"]` and `repo_context["root_path"]` instead of assuming `"."`.

### Remaining current-working-directory assumptions
These still exist and are currently intentional compatibility defaults, not hidden secondary pipelines:

| Location | Current assumption | Risk level | Notes |
| --- | --- | --- | --- |
| `services/repo_registry.py::ensure_default_repo` | default `root_path="."` | Medium | Safe for current self-repo flow, but cwd-sensitive |
| `services/repo_registry.py::resolve_repo` | default `fallback_root_path="."` | Medium | Compatibility default |
| `tools/repo_tools.py::_resolve_repo_runtime` | resolves `"."` to default self repo | Medium | Intended backward compatibility |
| `agents/root_agent.py::_resolve_repo_execution` | fallback root `"."` when repo_id omitted | Medium | Correct for current CLI, but should stay default-only |
| `services/repo_index_service.py::_sync_legacy_manifest` | compares against `Path(".").resolve()` | Medium | Legacy self-repo support only |
| `tools/repo_tools.py::_get_manifest_path` | falls back to `output/repo_manifest.json` when repo_id missing | Medium | Legacy compatibility path |
| `services/task_registry.py` | artifact workspace under `artifacts/tasks` relative to cwd | Low | Not repo mutation |

### Assessment
- Explicit `repo_id` execution is structurally sound.
- Backward compatibility still relies on cwd-sensitive defaults.
- Real mutation features must use resolved `repo_id`/`root_path` and must not rely on cwd defaults except as an explicit self-repo compatibility path.

## Identified risks (prioritized)

### 1. No single apply boundary
There is currently no `services/` mutation service that all writes must pass through. If implementation is added ad hoc in agents or tools, silent architectural drift is likely.

### 2. No dry-run contract
Nothing currently guarantees a plan-only execution mode for file mutation. Real apply without a mandatory dry-run layer would be unsafe.

### 3. Weak apply preconditions
Current `DraftSet` and `ChangeSet` contracts do not include file preconditions such as expected content hash or expected existence, so concurrent or stale writes could silently clobber changes.

### 4. Remaining cwd-sensitive compatibility paths
The self-repo compatibility flow still depends on `"."` in a few places. If mutation features accidentally use those defaults instead of explicit repo resolution, writes could target the wrong workspace.

### 5. Existing write utilities are outside implementation orchestration
`tools/report_tools.py` and `services/task_registry.py` already write files. They do not mutate repo source files today, but they prove the repository already has multiple write-capable codepaths. Mutation work must not casually reuse them.

### 6. No rollback or traceability model
There is no apply journal, no before/after hash capture, no rollback plan, and no per-file write result contract.

### 7. Legacy manifest sync could blur mutation boundaries
`output/repo_manifest.json` is still synced for the current repo. Future mutation/reporting code must keep artifact writes separate from source-file writes.

## Required invariants (must-follow rules)

These invariants should be treated as blockers before implementing Prompt 5-9.

1. No writes outside implementation mode.
2. `agents/root_agent.py` remains the only orchestration owner for implementation execution.
3. All repo source-file writes must go through one service under `services/`.
4. A mandatory `dry_run=True` path must exist before any real apply mode.
5. No apply operation may use cwd-based path resolution when a resolved `repo_id` exists.
6. Every write target must be validated as inside the resolved repo root.
7. No hidden writes may occur during spec, code-plan, change, draft, review, or repair stages.
8. Artifact writes and source-file writes must remain separate codepaths and separate result reporting.
9. Real apply must require explicit opt-in and must never happen as a side effect of analysis commands.
10. Every applied file must produce traceable before/after metadata.
11. Validation execution must be explicit, bounded, and tied to the same resolved repo.
12. Git diff reporting must read from the resolved repo only and must not become the write mechanism.

## Minimal contract design for apply layer

### `ApplyInput` should contain
- `repo_id: str`
- `root_path: str`
- `dry_run: bool`
- `source_artifact_type: str`
  - Example: `draft_set`
- `draft_set: DraftSet | None`
- `change_set: ChangeSet | None`
- `requested_operations: list[str]`
  - Example: `["create", "modify"]`
- `expected_repo_state: list[ApplyPrecondition]`
  - Per-file expected existence and expected content hash or indexed hash
- `request_summary: str`
- `initiator: str`
  - Example: `root_agent`
- `trace_id: str`
- `allow_overwrite: bool`
- `validation_commands: list[str]`
  - Optional and explicit, not implicit

### `ApplyResult` must contain
- `repo_id: str`
- `root_path: str`
- `dry_run: bool`
- `status: str`
  - Example: `planned`, `applied`, `blocked`, `failed`
- `planned_writes: list[AppliedFileResult]`
- `applied_writes: list[AppliedFileResult]`
- `blocked_writes: list[BlockedFileResult]`
- `warnings: list[str]`
- `errors: list[str]`
- `trace_id: str`
- `workspace_dirty_before: bool`
- `workspace_dirty_after: bool`
- `artifact_type: str`
- `validation_results: list[ValidationResult]`
- `git_diff_summary: str`
- `git_diff_paths: list[str]`

### Per-file guarantees that must exist
- The resolved absolute file path is inside the resolved repo root.
- The intended operation is explicit.
- The write is skipped or blocked if preconditions do not match.
- The result records whether the file was planned, changed, skipped, or blocked.
- The before hash and after hash are captured when possible.
- The returned result is enough to audit exactly what happened without re-running the apply.

## Ready / Not Ready verdict for implementing Prompt 5-9

### Verdict
Not Ready.

### Why
The architecture is ready for mutation work in the sense that the canonical orchestration, repo registry, repo indexing, typed contracts, and repo-aware context flow are all in place. However, it is not yet safe to implement real mutation features because the critical mutation boundary is missing:
- no apply service
- no dry-run contract
- no precondition/traceability model
- no explicit mutation invariants enforced in code

### Practical interpretation
- Safe to implement Prompt 5-9 next: yes, architecturally.
- Safe to turn on real filesystem mutation immediately: no.
- The next step should be the apply-layer contracts and single service boundary first, before any real file writes or implementation-mode apply behavior.
