# File Targeting Iteration

## What changed
- Expanded the single-repo file-targeting candidate pool with a configurable `targeting_candidate_pool_size`.
- Added an explicit deterministic reranking pass on top of the existing blended score.
- Added a bounded structural expansion pass that can pull in closely related companion files.
- Kept final writable-file selection bounded with a configurable `targeting_selected_file_limit`.

## Why
- The clean leakage-free baseline shows repo routing is already strong, while file targeting is the main bottleneck.
- The previous path narrowed too early and relied on a small top slice before bounded codegen.
- This iteration improves recall without changing repo routing or bounded-codegen hot-path behavior.

## Expected impact
- Better relevant-file recall for single-repo tasks.
- More resilient ranking when multiple candidates are close.
- A small, controlled way to include companion files that are commonly edited together.

## Tradeoffs
- Slightly larger candidate pool means a bit more file-targeting work per repo.
- Structural expansion is intentionally conservative and bounded, so it may miss some real dependencies.
- Final selection remains bounded to avoid widening write scope too aggressively.
