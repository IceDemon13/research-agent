# SDD Implementation Gap Analysis

## Scope

This audit treats the current repository architecture as the source of truth:

- `agents/root_agent.py` is the canonical orchestration entrypoint.
- `main.py` is only the thin executable wrapper.
- `tools/` is the canonical tool layer.
- `services/` is the canonical stateful/domain service layer.
- `contracts/` is the canonical typed contract layer.
- `formatters/` is the canonical rendering layer.

This document does not propose a parallel pipeline. It maps what already exists and what is still missing to reach a working spec-driven implementation pipeline that can safely operate beyond the current single-repo setup.

## Executive Summary

The repository already has a substantial internal SDD pipeline:

- repo-aware context building exists
- spec generation exists
- code planning exists
- change-set generation exists
- draft generation exists
- review and repair loops exist
- typed contracts exist for the main pipeline artifacts

The biggest missing pieces are not the planning stages themselves. The biggest gaps are operational:

- no canonical multi-repo or `repo_id` execution model
- no safe apply/write layer for generated drafts or changes
- no git diff reporting layer
- no validation runner integrated into the implementation pipeline
- no explicit user-facing implementation-mode dispatcher in the canonical root flow for `/pipeline`, `/changes`, and `/drafts`

## What Is Already Implemented

| Capability | Coverage | Exact modules / functions | Notes |
| --- | --- | --- | --- |
| Canonical orchestration entry | Implemented | `agents/root_agent.py::run_root_agent`, `agents/root_agent.py::build_agent`, `main.py::main` | `main.py` stays thin and delegates into the root agent path. |
| Shared LLM execution loop | Implemented | `loops/react_loop.py::run_react_loop` | Used by `spec_agent`, `code_agent`, `review_agent`, `repair_agent`, and other stage agents. |
| Repo-aware analysis / context shaping | Strongly implemented | `tools/repo_tools.py::parse_repo_query`, `build_context`, `resolve_repo_targets`, `select_candidate_files`, `find_symbol_occurrences`, `search_in_repo`, `sanitize_repo_context`, `build_repo_manifest`; `services/repo_context_rules.py::apply_repo_helper_create_rules`, `remove_readme_from_symbol_only_context`, `ensure_repo_impl_target`, `ensure_repo_impl_target_in_final_context`, `run_repo_context_rule_pipeline`; `agents/root_agent.py::_build_shared_repo_context`, `_prepare_final_repo_context_for_downstream` | This is the strongest existing part of the implementation pipeline. |
| Typed repo-context contract | Implemented | `contracts/repo_context_contract.py::RepoContextContract`, `normalize_repo_context` | Repo context still moves as dicts in many places, but normalization and a typed contract already exist. |
| Spec generation | Implemented | `agents/spec_agent.py::run_spec_agent`, `contracts/spec_contract.py::SpecContract`, `contracts/spec_parser.py`, `prompts/spec_prompt.py` | Spec generation is repo-context-aware via `ensure_repo_context` and `format_repo_context`. |
| Code planning | Implemented | `agents/code_agent.py::run_code_agent`, `run_code_agent_from_spec`, `_extract_patch_plan`; `contracts/spec_to_code_input.py::SpecToCodeInput`, `contracts/patch_plan.py::PatchPlan`, `contracts/file_change_plan.py::FileChangePlan`; `prompts/code_prompt.py` | The code stage produces a typed `PatchPlan`. |
| Change generation | Implemented | `agents/change_agent.py::run_change_agent`; `contracts/change_set.py::ChangeSet`, `contracts/proposed_file_change.py::ProposedFileChange`; `prompts/change_prompt.py` | Change generation already respects locked targets and manifest validation. |
| Draft generation | Implemented | `agents/draft_agent.py::run_draft_agent`; `contracts/draft_set.py::DraftSet`, `contracts/file_draft.py::FileDraft`; `prompts/draft_prompt.py` | This stage already contains strong safety logic, symbol/file locking, and patch-only fallback behavior. |
| Lightweight review-only flow | Implemented | `agents/root_agent.py::run_lightweight_review_pipeline`, `agents/review_agent.py::run_review_agent` | This is an explicit canonical review-only orchestration path. |
| Full review / repair loop | Implemented | `agents/root_agent.py::run_full_review_pipeline`, `agents/repair_agent.py::run_repair_agent`, `agents/review_agent.py::run_review_agent`, `contracts/review_result.py::ReviewResult`, `contracts/repair_result.py::RepairResult` | There is already a working repair loop with max-attempt control in root orchestration. |
| Internal spec-to-code / change / draft orchestration helpers | Implemented internally | `agents/root_agent.py::run_spec_only_pipeline`, `run_spec_to_code_pipeline`, `run_full_change_pipeline`, `run_full_draft_pipeline`, `run_full_review_pipeline` | These are the main building blocks for a future explicit implementation mode. |
| Repo manifest persistence | Partially implemented | `tools/repo_tools.py::build_repo_manifest`, `_load_repo_manifest` | Manifest persistence exists, but only as a singleton file in `output/repo_manifest.json`. |
| Document-level RAG / retrieval support | Implemented | `agents/research_agent.py::run_research_agent`, `agents/rag_research_agent.py`, `retriever.py::hybrid_search`, `ingest.py`, `contracts/retrieval_result.py::RetrievalResult` | Useful for repository help/commands/Q&A, not the core source-analysis path for code changes. |
| Output artifact writing | Narrowly implemented | `tools/report_tools.py::write_report`, `services/task_registry.py::write_task_artifact`, `services/spec_exporter.py::export_spec_to_markdown`, `services/brief_exporter.py::export_brief_to_markdown` | Safe output writing exists for reports/task artifacts, but not for applying generated repo changes. |
| Test surface for repo-context pipeline | Implemented | `tests/test_repo_commands.py`, `tests/repo_context_assertions.py`, `docs/TEST_WORKFLOW.md`, `scripts/run_test.ps1`, `scripts/run_repo_context_tests.ps1`, `scripts/run_all_tests.ps1` | The repo already has a natural place for validation, but the implementation pipeline does not invoke it automatically. |

## Gap Analysis By Missing Capability

### 1. Onboarding a second repo/workspace

Status: Partially covered, but not operationally supported.

Existing coverage:

- Many repo tools already accept `root_path`, for example `tools/repo_tools.py::build_context`, `search_in_repo`, `find_symbol_occurrences`, and `build_repo_manifest`.
- Manifest data stores the resolved repo root in `build_repo_manifest`.
- `services/task_registry.py` creates task workspaces under `artifacts/tasks`, but those are task artifact folders, not source repo registrations.

Actual gap:

- `agents/root_agent.py` hardcodes `"."` when building repo context and downstream context.
- There is no `repo_id`, repo registry, or repo selection contract anywhere in `contracts/`.
- There is no canonical service that maps a stable repo identity to a root path and metadata.
- There is no safe UX/API for selecting “current repo” versus “target repo”.

### 2. Persistent repo indexing

Status: Partially covered.

Existing coverage:

- `tools/repo_tools.py::build_repo_manifest` persists a manifest to `output/repo_manifest.json`.
- `ingest.py` + `retriever.py` persist a FAISS/BM25 document index under `index/`.

Actual gap:

- The repo manifest is a single shared file path, not namespaced by repo or `repo_id`.
- There is no persistent code embedding index for source-repo execution.
- There is no incremental refresh or invalidation service.
- There is no service-layer API for “ensure repo index exists for repo X”.

### 3. Explicit `repo_id`-based execution

Status: Missing.

Existing coverage:

- None at the contract/orchestration level.

Actual gap:

- No `repo_id` field in `RepoContextContract`, `AgentResult`, `RouteResult`, `SpecToCodeInput`, `PatchPlan`, `ChangeSet`, or `DraftSet`.
- No repo registry service in `services/`.
- No root-agent API that accepts a repo identifier or resolved repo descriptor.

### 4. Safe file writing

Status: Missing for source-repo mutation.

Existing coverage:

- `tools/report_tools.py::write_report` safely writes markdown reports to output.
- `services/task_registry.py::write_task_artifact` safely writes artifacts under `artifacts/tasks`.
- `draft_agent` and `change_agent` generate structured outputs describing intended edits.

Actual gap:

- There is no canonical `services/` layer for writing source-repo files.
- There is no dry-run apply contract.
- There is no backup/rollback/conflict detection flow.
- There is no “write only if target still matches expected context” guard.

### 5. Applying generated drafts/changes to a repo

Status: Missing.

Existing coverage:

- `ChangeSet` and `DraftSet` are structured enough to be inputs to a future applier.
- `draft_agent` already supports symbol-only and patch-only fallback output modes.

Actual gap:

- There is no `apply_change_set(...)` or `apply_draft_set(...)` service.
- There is no canonical “implementation mode” agent/service that turns `ChangeSet` or `DraftSet` into file mutations.
- There is no root-agent route that performs a controlled apply step.

### 6. Git diff reporting

Status: Missing.

Existing coverage:

- None beyond human-readable draft/change descriptions.

Actual gap:

- No git service in `services/` or `tools/`.
- No typed diff/report contract.
- No post-apply diff summary in the root orchestration flow.

### 7. Validation commands

Status: Partially covered.

Existing coverage:

- Validation scripts exist in `scripts/run_test.ps1`, `scripts/run_repo_context_tests.ps1`, and `scripts/run_all_tests.ps1`.
- Validation guidance exists in `docs/TEST_WORKFLOW.md`.
- `PatchPlan`, `ChangeSet`, and `DraftSet` already carry textual `checks`.
- `review_agent` already performs deterministic prechecks before semantic review.

Actual gap:

- There is no typed validation contract/result for “run these commands, capture exit codes, summarize output”.
- There is no canonical validation service in `services/`.
- Root-agent implementation helpers do not automatically transition into a validation step.

### 8. Explicit implementation mode orchestration

Status: Partially covered internally, not fully exposed canonically.

Existing coverage:

- `agents/root_agent.py::run_spec_to_code_pipeline`
- `agents/root_agent.py::run_full_change_pipeline`
- `agents/root_agent.py::run_full_draft_pipeline`
- `agents/root_agent.py::run_full_review_pipeline`
- `README.md` and `startup_text.py` describe `/pipeline`, `/changes`, `/drafts`, and `/spec`

Actual gap:

- `agents/root_agent.py::run_root_agent` does not serve as a single explicit dispatcher for `/pipeline`, `/changes`, and `/drafts`.
- The implementation helpers exist and are tested directly, but the canonical entry flow still mainly routes to `research`, `spec`, `code`, `jira`, or `/review`.
- This means the implementation pipeline exists more as internal orchestration helpers than as one canonical user-facing implementation mode.

## RAG / Retrieval Relevance To Repo-Aware Behavior

RAG support exists and is useful, but it is not the core implementation path for source-repo mutation.

Implemented:

- `retriever.py` and `ingest.py` provide persistent document retrieval.
- `agents/rag_research_agent.py` answers repository-help questions, command questions, and explanatory queries.
- `contracts/retrieval_result.py` provides a typed retrieval result contract.

Important limitation:

- The implementation pipeline is grounded primarily by `tools/repo_tools.py` + `services/repo_context_rules.py`, not by `retriever.py`.
- Future implementation work should not create a second “repo-aware implementation path” through RAG. RAG should remain supportive unless the repository explicitly chooses to unify those paths.

## Recommended Implementation Order

1. Add explicit repo identity without changing the canonical pipeline shape.

- Add a typed repo descriptor contract in `contracts/`, such as `RepoRef` or `ExecutionTarget`.
- Add a canonical repo registry service in `services/` that resolves `repo_id -> root_path`.
- Thread that into `agents/root_agent.py` while keeping root agent as the only orchestration entrypoint.

2. Make the canonical root flow expose implementation modes explicitly.

- Extend `agents/root_agent.py::run_root_agent` so `/pipeline`, `/changes`, and `/drafts` dispatch through the existing helper functions instead of leaving them as mostly internal helpers.
- Do not create a new top-level executor or a second orchestration module.

3. Introduce a safe apply layer in `services/`.

- Add a typed apply request/result contract in `contracts/`.
- Add a repo mutation service in `services/` that supports dry-run by default.
- Accept `DraftSet` and later `ChangeSet` as input, and make mutations explicit, reviewable, and opt-in.

4. Add post-apply diff reporting.

- Add a small git/report service in `services/`.
- Produce a typed diff summary contract and formatter output.
- Keep this as a child step of the root-agent implementation flow, not a separate pipeline.

5. Add validation execution as a first-class service.

- Add a typed validation result contract in `contracts/`.
- Add a validation runner service in `services/` that executes known commands and returns bounded results.
- Start with existing scripts in `scripts/` and current unittest-based workflows.

6. Only after repo identity exists, add persistent multi-repo indexing.

- Namespace manifest and retrieval/index artifacts by `repo_id`.
- Avoid bolting a second index-orchestrator path beside the root agent.
- Keep repo indexing as infrastructure used by the canonical root flow.

## Architectural Risks To Avoid

### Risk: creating a parallel implementation pipeline

What it would look like:

- a new top-level `pipeline/` or `implementation/` package that bypasses `agents/root_agent.py`
- a second orchestration entry that duplicates `run_spec_to_code_pipeline` and related helpers

Why it is risky:

- It would reintroduce the architectural duplication already cleaned up in this repo.

Recommended guardrail:

- Extend `agents/root_agent.py` and its existing helpers instead of adding a parallel executor.

### Risk: creating a second repo-awareness system

What it would look like:

- using RAG retrieval as a separate implementation-context source in parallel with `tools/repo_tools.py`
- duplicating repo selection logic outside the existing repo-context rules

Why it is risky:

- The repo already has a deterministic repo-context shaping pipeline. A second one would drift.

Recommended guardrail:

- Keep `tools/repo_tools.py` + `services/repo_context_rules.py` as the canonical repo-analysis path for implementation.

### Risk: introducing untyped apply/validation behavior

What it would look like:

- raw dict payloads for apply steps
- ad hoc shell commands embedded in agents without typed results

Why it is risky:

- It would make mutating behavior harder to review and test.

Recommended guardrail:

- Add typed contracts first, then service-layer execution, then root-agent orchestration.

## Top Missing Capabilities

1. Explicit `repo_id` / multi-repo execution model
2. Safe apply/write service for `DraftSet` and later `ChangeSet`
3. Canonical root-agent dispatch for implementation modes
4. Git diff reporting after apply
5. Validation runner integrated into the implementation pipeline
6. Namespaced persistent repo indexing
