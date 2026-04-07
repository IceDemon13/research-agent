# Canonical Mainline Codegen State

Date: `2026-04-04`

## A. Current mainline state

Grounding, bounded planning, and bounded codegen are now stable enough to treat as the mainline for current production-oriented research on the good slice. The trusted line is:

- grounded file/class selection remains fixed before codegen
- bounded generation prompt/context observability is persisted
- prompt/context equality can be checked with comparison-mode normalization
- validation for the Telemart desktop family uses the runner-backed build path
- same-file concentration and narrow compile-hardening are active enough to make `TEL-13491` interpretable again

Current good slice summary:

- `TEL-13491` is now a build-valid, test-env-blocked downstream case after the concrete-symbol retry prompt and local durable-state work
- `TEL-13458` remains a build-valid, test-env-blocked downstream case

## B. Proven solved-enough items

- bounded generation observability now persists prompt text, build-context payload, hashes, lengths, provider/model metadata, and contract version
- prompt/context comparison observability is now good enough to prove drift vs equality, including comparison-mode normalization for telemetry-only noise
- runner-backed build path for `telemart_soft_test` is fixed enough to avoid the old local Linux-side `dotnet` fallback
- computed-validation retry prompt is now concrete-symbol aware for `TEL-13491`-like cases and no longer relies on generic placeholders
- local durable-state fix for `TEL-13491` exists in `OrderPackCellViewModel` via local `HashSet<int>` state plus synchronized `WarehouseCells`
- current `TEL-13491` bounded replay status is `valid_build_env_blocked`

## C. Explicitly shelved / not mainline blockers

- long-tail cases outside the current good slice
- broad prompt churn outside narrow proven retry paths
- broad multi-file expansion as a default recovery strategy
- GitNexus UI as a non-blocking surface
- `TEL-13394` as a long-tail accepted exception, not a mainline blocker

## D. Current good slice

- `TEL-13491` -> `valid_build_env_blocked`
- `TEL-13458` -> `valid_build_env_blocked`

## E. Remaining known blockers outside mainline

- unrelated targeted-test environment blockers, including missing desktop runtime/test assets
- repo-specific runner or validation-environment gaps that still block behavior-proof even when restore/build pass
- downstream behavioral correctness is not yet generalized enough for broad production claims beyond the current good slice

## F. Exact next recommended focus

The next focus should stay on downstream patch-quality measurement for the current good slice, starting with behavior-proof for build-valid cases once the unrelated test-environment blockers are removed, rather than reopening broad grounding, routing, or multi-file heuristic work.
