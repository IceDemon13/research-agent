# Canonical Mainline Grounding/Planning Status

Date: `2026-04-03`

## Canonical Mainline

This document freezes the current working grounding/planning line as the canonical mainline for follow-on research.

Canonical mainline characteristics:

- system-selected file is fixed by grounding, not chosen by the LLM
- selected-file symbol extraction is active and auditable
- grounded class/method lists are exposed in the spec/planning path
- class/method validation is enforced against grounded lists
- patch transport uses sentinel blocks rather than fragile JSON framing
- GitNexus is usable as an optional provider, but is not required for the mainline to work
- family-aware deterministic local retrieval is active
- Streamline family is recovered at file/class level
- Monobank family remains partially resistant in one long-tail case

Current trusted 5-ticket weak-plan slice status:

- exact benchmark file: `4/5`
- exact benchmark class: `4/5`
- executable plans: `4/5`
- remaining stable upstream wrong-file exception: `TEL-13394`

## Good Slice

For current patch/codegen-quality research, the good slice is:

- `TEL-13491`
- `TEL-13458`
- `TEL-13375`
- `TEL-13502`

Definition:

- tickets where benchmark file/class are already correct or effectively correct in the current canonical mainline
- excludes `TEL-13394`

## Shelved Lines

The following lines are intentionally shelved and should not be treated as mainline blockers:

- broad planner prompt churn after file fixing
- broad multi-file companion expansion
- repeated Monobank broad retrieval churn beyond narrow family-specific refinements
- GitNexus web UI as a primary blocker
- TEL-13394 as a broad-system blocker

## TEL-13394 Exception Status

`TEL-13394` is now treated as a long-tail exception.

Reason:

- the failure is no longer broad grounding collapse
- `OrderRepository.cs` now enters the candidate set
- interface-level repository noise has been reduced
- handler/request dominance has been reduced
- further narrow Monobank repository-vs-repository refinements did not produce material improvement

Operational decision:

- keep the current mainline
- do not block mainline patch/codegen research on `TEL-13394`
- treat `TEL-13394` as a tracked exception rather than a gating failure

## Recommended Next Focus

The exact next experiment should be:

- focused patch/codegen-quality research on the good slice
