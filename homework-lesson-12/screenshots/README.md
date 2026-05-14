# Screenshots checklist

The HW12 brief asks for **4 screenshots from the Langfuse UI**. Place each
file in this folder using the exact name below — the grader will be looking
for them.

> Tip: take screenshots **after** running `python main.py --batch --quick` so
> you have 5 traces in your project and the dashboards look populated.

---

## 1. `01_trace_tree.png` — full trace tree with sub-agents and tool calls

**What to capture:** Open one of your traces and screenshot the **left-hand
trace tree** showing the nested structure.

**How to get there:**
1. Langfuse UI → **Tracing → Traces** → click the most recent trace named
   `multi-agent-run`.
2. Make sure the left panel is expanded so you can see at least:
   - `multi-agent-run` (parent)
     - `agent.planner`
     - `agent.research`
       - `tool.search_web` (one or more)
       - `tool.retrieve_local_context` (one or more)
       - the underlying `ChatOpenAI` LLM generations
     - `agent.critic`
3. Screenshot the entire window. The depth of the tree is what proves
   requirement #1 (full sub-agent and tool nesting).

---

## 2. `02_session.png` — session with multiple traces

**What to capture:** A Langfuse **Session** detail page listing several traces
that share the same `session_id`.

**How to get there:**
1. Langfuse UI → **Sessions**.
2. Click the session named `hw12-session-<hash>` (created by `main.py`).
3. The detail page shows: session id, user_id, and a list of all traces
   inside that session.
4. Screenshot the page so both **the session header (with user_id)** and
   **the trace list** are visible.

This single screenshot proves requirements #2 (Session) and the User
association in one go. (Optionally take a second screenshot of
**Users → dmytro@uni** as bonus evidence.)

---

## 3. `03_prompts.png` — Prompt Management

**What to capture:** The **Prompts** page listing all four uploaded prompts.

**How to get there:**
1. Langfuse UI → **Prompts**.
2. Confirm you can see four prompts created by `seed_prompts.py`:
   - `hw12/planner`
   - `hw12/research`
   - `hw12/critic`
   - `hw12/save_supervisor`
3. (Optional, even better) click `hw12/research` and screenshot the prompt
   detail page showing the version history + `production` label.

This proves requirement #3.

---

## 4. `04_evaluator_scores.png` — LLM-as-a-Judge scores on a trace

**What to capture:** A trace detail view with the **Scores** tab open,
showing **at least 2 evaluators** have written scores on the trace.

**How to get there:**
1. After running the system, wait 1–2 minutes for Langfuse to run the
   evaluators asynchronously.
2. Langfuse UI → **Tracing → Traces** → click a recent trace.
3. Open the **Scores** tab (or scroll the right-hand panel until you see
   the scores section).
4. You should see at least two of:
   - `relevance-1-to-5` (numeric)
   - `groundedness-true-false` (boolean)
   - `report-structure` (categorical)
5. Screenshot the panel so all score names + values + reasoning are visible.

This proves requirement #4.

---

## File naming summary

```
screenshots/
├── 01_trace_tree.png
├── 02_session.png
├── 03_prompts.png
└── 04_evaluator_scores.png
```

Anything extra (e.g. `05_evaluator_dashboard.png` showing evaluator status with
"X traces processed") is welcome bonus evidence but not required for max
grade.
