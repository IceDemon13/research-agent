# LLM-as-a-Judge evaluators

Copy-paste-ready evaluator definitions for **Langfuse UI →
LLM-as-a-Judge → Evaluators → + Set up evaluator**.

The HW12 spec requires **at least 2 evaluators with different score types**.
This folder gives you **3 production-ready evaluators** so you have one extra
in case anything misfires during grading:

| File | Target trace | Score type | Output |
|------|--------------|------------|--------|
| `01_relevance_numeric.md` | `multi-agent-run` | **NUMERIC** | 1.0 – 5.0 |
| `02_groundedness_boolean.md` | `multi-agent-run` | **BOOLEAN** | true / false |
| `03_report_structure_categorical.md` | `multi-agent-run` | **CATEGORICAL** | excellent / acceptable / poor |

Quick setup steps (repeat for each file):

1. Open **LLM-as-a-Judge → Evaluators → + Set up evaluator**.
2. Pick **Create new template** (or reuse "custom").
3. Paste the prompt text from the file under **Prompt**.
4. Match the **Score type** and **Score categories** as listed at the top of
   each file.
5. Under **Target** choose **New traces**, scope to **`multi-agent-run`**
   (or "All traces" if you prefer broader coverage), and set **Sample rate**
   to `1.0` so every trace is judged.
6. Map template variables to trace fields:
   - `{{input}}` → `Trace input`
   - `{{output}}` → `Trace output`
7. Save. Within ~1–2 minutes after the next MAS run the evaluator
   produces a score visible under the trace's **Scores** tab.

> 💡 In the supervisor, the trace `input` is the topic and `output` is a JSON
> object `{verdict, revision_count, report_markdown}`. The evaluator prompts
> below explicitly reference `report_markdown` so the judge sees the actual
> deliverable, not just the verdict label.
