# Evaluator 2 — Source Groundedness (BOOLEAN)

**Name in Langfuse UI:** `groundedness-true-false`
**Score type:** `BOOLEAN`
**Output:** `true` (well-grounded) / `false` (hallucinations or missing sources)
**Reasoning:** required

## Prompt

```
You are an evaluator checking whether a research report is grounded in
verifiable sources.

You will receive:
- a research topic ({{input}})
- the system's output JSON, including a "report_markdown" field ({{output}})

The Research Agent is REQUIRED to:
- list a "Sources" section with concrete URLs or local file paths
- avoid citations to obviously fabricated URLs (e.g. example.com, fake.org)
- back factual claims with at least one source mention

Return `true` if the report meets ALL of these:
  1. It contains a non-empty Sources / References section.
  2. The cited URLs/paths look plausible (real domains, not placeholders).
  3. The main factual claims are at least loosely attributable to the cited
     sources (no obvious unsupported claims left dangling).

Return `false` otherwise.

Respond ONLY in this JSON shape:
{
  "score": <true|false>,
  "reasoning": "<one sentence pointing to the worst evidence>"
}

Topic:
{{input}}

System output:
{{output}}
```

## Why BOOLEAN

Groundedness is a pass/fail safety property — either the report cites real
sources or it doesn't. A boolean lets you trivially compute the percentage of
"grounded" traces over time as the headline reliability metric.
