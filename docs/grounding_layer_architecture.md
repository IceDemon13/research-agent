# Grounding Layer Architecture

## Goal
Introduce a planning-only grounding layer between Jira task text and implementation planning so GitNexus is optional and never a single point of failure.

## Data Flow
1. Jira task text enters the existing planning path.
2. Existing repo context is still resolved first.
3. `GroundingService.build_planning_grounding(...)` receives:
   - `repo_id`
   - Jira task payload
   - current candidate files
   - current repo context
4. Providers contribute bounded grounding evidence.
5. The service merges provider output into one `GroundingContext`.
6. The planning prompt consumes:
   - repo profile
   - grounded candidate files
   - grounded candidate symbols
   - grounding diagnostics summary
7. The model must still return exact file, class, and method.
8. If the returned file is not in the repo, planning fails closed.

## Provider Responsibilities

### RepoProfileProvider
- Loads repo metadata and stack hints.
- Uses existing local repo profile/index data when available.
- Supplies repo name, tech stack, framework markers, and project structure hints.

### TreeSitterSymbolProvider
- Planning-only local symbol resolver.
- Current implementation is a lightweight local static scan over the bounded candidate files.
- Produces exact class/method candidates when it can.
- Does not affect routing or file targeting.

### EmbeddingRecallProvider
- Scaffold only in this diff.
- Exposes interface and diagnostics but does not add recall yet.
- Keeps room for future local grounding expansion without changing the planning consumer shape.

### GitNexusProvider
- Optional enrichment provider.
- Exposes health and index state:
  - `available`
  - `indexed`
  - `query_succeeded`
- Fails soft:
  - unavailable GitNexus does not block planning
  - unindexed GitNexus does not block planning
  - query failure does not block planning

## Fallback Order
1. Existing local repo context and current candidate files
2. Repo profile metadata
3. Local symbol grounding
4. Optional GitNexus enrichment
5. Placeholder embedding recall

The merge policy is intentionally bounded:
- deterministic and local signals first
- top grounded files only
- exact symbol candidates when available
- provider diagnostics always preserved

## Why GitNexus Is Optional
- Current GitNexus health/index state can be unhealthy or missing.
- Planning should still have repo grounding from local signals.
- This prevents GitNexus from being a hidden hard dependency for planning quality.

## What Should Be Implemented Next
- Replace the lightweight local symbol scan with a stronger tree-sitter-backed extractor.
- Add bounded local embedding recall behind the existing provider interface.
- Add deeper grounding-quality telemetry to planning evaluation only after the scaffold is stable.

## Intentionally Not Changed In This Diff
- Routing
- File targeting hot path
- Target arbitration
- Bounded codegen hot path
- Evaluation logic
- GitNexus UI/build repair
