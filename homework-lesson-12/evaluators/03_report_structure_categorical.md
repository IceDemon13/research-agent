# Evaluator 3 — Report Structure (CATEGORICAL)

**Name in Langfuse UI:** `report-structure`
**Score type:** `CATEGORICAL`
**Categories:** `excellent`, `acceptable`, `poor`
**Reasoning:** required

## Prompt

```
You are evaluating whether a research report follows the required structure.

The Research Agent was instructed to produce markdown with these sections:
- # Title
- ## Executive summary
- ## Findings
- ## Limitations
- ## Sources

You will receive:
- the original topic: {{input}}
- the system output JSON containing "report_markdown": {{output}}

Score with one of these three categories:

- "excellent"  — all 5 sections present, headings well-formed, content
                 substantive in each section, sources are concrete.
- "acceptable" — 3 or 4 of the 5 sections present, or content is thin in
                 one section but the skeleton is intact.
- "poor"       — fewer than 3 sections present, headings missing/garbled,
                 or large parts of the report are filler text.

Return ONLY this JSON:
{
  "score": "<excellent|acceptable|poor>",
  "reasoning": "<one sentence stating which sections are present/missing>"
}

Topic:
{{input}}

System output:
{{output}}
```

## Why CATEGORICAL

Structure is a discrete property — sections are either there or not. Three
labels are easier to inspect at a glance than a numeric score and map cleanly
to the rubric the Critic agent itself uses.
