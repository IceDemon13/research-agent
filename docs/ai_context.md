# TELEMART AI Context

## 1. Architecture Overview

### Entry points
- `web_app.py`: main FastAPI app, HTTP API, `/ui/*` static pages, auth, workflow endpoints, run history endpoints.
- `main.py` / `console_app.py`: CLI-style entrypoints.
- `agents/root_agent.py`: canonical orchestration layer for repo-aware agent flows.

### Main layers
- `agents/`: specialized LLM-driven agents and orchestration helpers.
- `services/`: integration and stateful business services.
- `contracts/`: typed data models for workflows, execution, runs, validation, apply, review.
- `static/`: product UI pages (`index.html`, `workflow.html`, `runs.html`, admin pages).
- `artifacts/`: persisted workflow outputs, review/execution/apply artifacts, temp workspaces, run state.

### Agents
- `root_agent.py`: top-level orchestration and routing.
- `jira_agent.py`: Jira-oriented task understanding.
- `spec_agent.py` / `spec_review_agent.py`: spec generation and review.
- `draft_agent.py`: draft/change proposal generation.
- `review_agent.py`: review and issue finding.
- `code_agent.py` / `change_agent.py`: code-focused execution paths.
- `repair_agent.py`: bounded repair loop support.
- `research_agent.py` / `rag_research_agent.py`: repo-aware research/retrieval flows.

### Orchestration flow
- User/UI calls `web_app.py`.
- Backend builds workflow result models from `contracts/workflow_contract.py`.
- Repo intelligence and Jira/task context are gathered.
- Draft patch can be generated, reviewed, validated, then applied through bounded apply modes.
- Persisted artifacts in `artifacts/` are treated as operational truth for review/execution/apply continuity.

## 2. Key Flows

### `analyze_task`
- Endpoint: `POST /workflows/analyze-task`
- Purpose: understand Jira task, detect repo, estimate task quality/confidence/novelty, surface candidate files and questions.
- Main output: `AnalyzeTaskWorkflowResult`.

### `implementation_plan`
- Endpoint: `POST /workflows/implementation-plan`
- Purpose: turn analysis into an implementation plan with likely files, risks, and validation intent.
- Main output: `ImplementationPlanWorkflowResult`.

### `generate_draft_patch`
- Endpoint: `POST /workflows/generate-draft-patch`
- Purpose: build bounded draft patch context, allowed files, diff preview, file rationales, and validation plan.
- Main output: `DraftPatchWorkflowResult` in draft-ready state.

### `review`
- Endpoint: `POST /workflows/review-draft-patch`
- Purpose: persist review decision and, on approval, trigger bounded execution/validation of the approved draft.
- Important rule: review/apply readiness must reflect the persisted execution artifact, not optimistic UI or route-local assumptions.

### `apply`
- Endpoint: `POST /workflows/apply-draft-patch`
- Purpose: apply only a validated persisted draft execution artifact.
- Supported modes in current product surface:
  - `dry_apply`
  - `workspace_apply`
  - `local_apply`
- Safety: apply is bounded to allowed files and guarded by persisted execution invariants.

## 3. Important Services

### `services/validation_service.py`
- Central validation entrypoint.
- Chooses local validation vs validation-runner execution.
- Uses `RepositoryRegistryService` and `RepoValidationService`.
- Produces `ValidationResult` with command/step status, failed tests, stdout/stderr, and runtime metadata.

### `services/draft_patch_execution_service.py`
- Executes approved draft patches in a temp workspace.
- Uses:
  - `TempWorkspaceService`
  - `ApplyService`
  - `DiffService`
  - `ValidationService`
  - bounded repair logic from `repair_agent`
- Persists execution artifacts under `artifacts/draft_patch_executions`.
- Core responsibilities:
  - baseline validation
  - patched validation
  - regression map calculation
  - bounded repair attempts
  - final diff/apply_input production for later apply

### `services/repo_intelligence_service.py`
- High-level repo intelligence coordinator.
- Combines:
  - native repo index/memory services
  - multi-repo routing and file targeting
  - optional GitNexus bridge/index services
  - historical change memory
- Used to improve repo selection, file targeting, and implementation grounding.

### GitNexus integration
- `services/gitnexus_index_service.py`: indexing/reindex control, repo visibility, runtime guardrails.
- `services/gitnexus_bridge_service.py`: query bridge between app flows and GitNexus outputs.
- Current behavior:
  - GitNexus is optional and allowlist-driven.
  - Temporary runtime repos/workspaces are intentionally excluded from GitNexus indexing.
  - Native repo intelligence remains fallback/default behavior.

## 4. Contracts

### `contracts/draft_patch_execution_contract.py`
- Defines:
  - `DraftPatchExecutionHandoff`
  - `DraftPatchExecutionResult`
  - `DraftPatchRegressionMap`
  - `DraftPatchValidationSnapshot`
  - `ApplyInputPayload`
- Key invariant theme:
  - execution/apply state must be internally consistent
  - `apply_ready` requires validated execution and empty blockers
  - out-of-bounds or failed validation states cannot claim apply readiness

### `contracts/workflow_contract.py`
- Defines product-facing workflow result models:
  - `AnalyzeTaskWorkflowResult`
  - `ImplementationPlanWorkflowResult`
  - `DraftPatchWorkflowResult`
  - supporting items for candidates, plan preview, review issues, file rationales, branches
- These models are the shape used by UI/API for the main AI Delivery Flow.

## 5. UI

### `static/workflow.html`
- Primary unified flow page.
- Product flow:
  - Analyze
  - Plan
  - Draft
  - Review
  - Apply
- Uses persisted run state and run restore via `run_id`.
- Main backend endpoints used:
  - `POST /workflows/analyze-task`
  - `POST /workflows/implementation-plan`
  - `POST /workflows/generate-draft-patch`
  - `POST /workflows/review-draft-patch`
  - `POST /workflows/apply-draft-patch`
  - `GET /flow-runs/{run_id}`

### `static/runs.html`
- Runs Dashboard for flow history and continuation.
- Uses:
  - `GET /flow-runs`
  - `GET /flow-runs/{run_id}`
  - `GET /flow-runs/artifacts/view`

### `static/index.html`
- Landing page.
- Promotes unified AI Delivery Flow and recent runs.

## 6. Environment and Runtime

### Docker services
- `app`: FastAPI app on port `8000`
- `postgres`: PostgreSQL on port `5432`
- `validation-runner`: external validation service, health on port `8091`
- `gitnexus`: GitNexus sidecar on port `3010` and MCP/internal port `3011`
- `gitnexus-web`: optional UI on port `5173`

### Important ports
- `8000`: main web app/UI/API
- `5432`: Postgres
- `8091`: validation runner
- `3010`: GitNexus
- `3011`: GitNexus MCP/internal
- `5173`: GitNexus web UI

### Important env vars
- LLM/auth:
  - `OPENAI_API_KEY`
  - `OPENROUTER_API_KEY`
  - `MODEL_NAME`
- Validation/runtime:
  - `VALIDATION_RUNNER_ENABLED`
  - `VALIDATION_RUNNER_BASE_URL`
  - `VALIDATION_RUNNER_TYPE`
  - `VALIDATION_RUNNER_TIMEOUT_SECONDS`
  - `REPO_CLONE_ROOT`
- Repo intelligence:
  - `REPO_INTELLIGENCE_PROVIDER`
  - `GITNEXUS_ENABLED`
  - `GITNEXUS_REPO_ALLOWLIST`
  - `GITNEXUS_INTERNAL_BASE_URL`
  - `GITNEXUS_EXTERNAL_BASE_URL` / `GITNEXUS_EXTERNAL_UI_URL`
- Apply/publishing guards:
  - `ALLOW_REAL_APPLY`
  - `ALLOW_PR_CREATION`
  - `ALLOW_REVIEW_CREATION`
- Persistence/auth:
  - `POSTGRES_DSN`
  - `BOOTSTRAP_ADMIN_USERNAME`
  - `BOOTSTRAP_ADMIN_PASSWORD`
  - `ALLOW_DEV_LOGIN`

## 7. Practical Notes
- The current product truth for review/apply readiness is the persisted execution artifact, not transient UI state.
- `workspace_apply` is the preferred real apply mode for bounded commits in a clean workspace.
- Runs Dashboard and workflow resume rely on persisted run/review/execution/apply artifacts surviving reloads and restarts.
