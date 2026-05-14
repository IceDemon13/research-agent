# Local context seed

This file exists so `retrieve_local_context` has at least one document
to return on a fresh checkout. The Research Agent will treat it as one of
its sources when relevant.

## Multi-agent observability

- Tracing groups every sub-agent call (planner, research, critic) under a
  single parent trace, enabling debugging of the full Plan → Research →
  Critique pipeline.
- Session-level grouping is the recommended way to track multi-turn
  conversations or batch runs that share intent.
- Prompt Management decouples prompt iteration from code releases: you can
  ship a new prompt version without redeploying the agent.

## LLM-as-a-Judge

- Numeric scores are best for ranking; boolean scores are best for pass/fail
  safety properties; categorical scores are best for rubric-style evaluation.
- Online evaluators run asynchronously in the background — expect 1-2 minutes
  before scores appear on a freshly created trace.
