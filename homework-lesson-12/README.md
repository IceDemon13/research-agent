# Homework 12 — Langfuse observability for the multi-agent system

This homework wraps the HW10 multi-agent research assistant
(**Planner → Research → Critic → HITL save**) with full Langfuse
observability:

| Spec requirement | Where it lives |
|---|---|
| 1. Every MAS run → trace with full sub-agent/tool tree | `supervisor.py` (`@observe`) + `agents/*` (`@observe` + LangChain `CallbackHandler`) + `tools.py` (`@observe`) |
| 2. Traces grouped in a Session + tagged with `user_id` | `supervisor.run()` calls `langfuse_setup.update_trace(session_id=..., user_id=..., tags=...)` |
| 3. All system prompts in Langfuse Prompt Management | `prompts.py` (loader, fetches by name+`production` label) + `seed_prompts.py` (one-shot uploader). **No hardcoded system prompts in any `agents/*.py` file.** |
| 4. ≥2 LLM-as-a-Judge evaluators auto-scoring traces | `evaluators/` (3 ready-to-paste evaluator definitions: numeric, boolean, categorical) |
| 5. 4 screenshots from Langfuse UI | `screenshots/README.md` (exact instructions and filenames) |

---

## 0. Prerequisites

- Python 3.11+
- An OpenAI API key (`OPENAI_API_KEY`)
- A free Langfuse Cloud account at <https://us.cloud.langfuse.com>

## 1. Setup

```powershell
cd homework-lesson-12

# Create / activate venv (re-uses parent project's venv if already active)
python -m venv .venv
.\.venv\Scripts\Activate.ps1

pip install -r requirements.txt
```

Create `.env` in `homework-lesson-12/` (copy from `.env.example`):

```
OPENAI_API_KEY=sk-...
LANGFUSE_PUBLIC_KEY=pk-lf-...
LANGFUSE_SECRET_KEY=sk-lf-...
LANGFUSE_BASE_URL=https://us.cloud.langfuse.com
```

> Get the Langfuse keys from
> **Langfuse UI → Settings → API Keys → + Create new API keys**, after you
> create an Organization + Project (suggested name: `homework-12`).

## 2. Upload the agent prompts to Langfuse Prompt Management

This is the one-time step that moves every system prompt from the codebase
into Langfuse:

```powershell
python seed_prompts.py
```

You should see four prompts uploaded:

```
Uploading prompt: hw12/planner (label=['production']) ...
Uploading prompt: hw12/research (label=['production']) ...
Uploading prompt: hw12/critic (label=['production']) ...
Uploading prompt: hw12/save_supervisor (label=['production']) ...
Done. Check the Langfuse UI -> Prompts to confirm.
```

After this step the codebase contains **no hardcoded system prompts** in any
`agents/*.py` file — they are all pulled from Langfuse at agent construction
time via `prompts.get_prompt(name, **template_vars)`.

> If you change a prompt in the Langfuse UI, just promote the new version to
> the `production` label and restart the CLI — no code change needed.

## 3. Build the local retrieval index (optional but recommended)

The Research Agent can call a local retriever in addition to web search.
Drop `.md` / `.txt` / `.pdf` files in `data/` (a `seed_notes.md` is already
there) and run:

```powershell
python ingest.py --rebuild
```

## 4. Configure the LLM-as-a-Judge evaluators

Open `evaluators/README.md`. It points to three ready-made evaluators with
**different score types** (numeric / boolean / categorical) — paste each
into **Langfuse UI → LLM-as-a-Judge → Evaluators → + Set up evaluator**:

| File | Score type | Output |
|---|---|---|
| `01_relevance_numeric.md` | NUMERIC | 1.0–5.0 |
| `02_groundedness_boolean.md` | BOOLEAN | true / false |
| `03_report_structure_categorical.md` | CATEGORICAL | excellent / acceptable / poor |

Scope each evaluator to **target trace `multi-agent-run`** (or "All traces")
with **sample rate `1.0`** so every new run gets evaluated. The HW spec
requires at least 2 — we set up 3 to be safe.

## 5. Run the system to generate traces

The easiest way to populate everything for the screenshots is the batch
mode — it runs 5 diverse topics back-to-back in one Langfuse session:

```powershell
python main.py --batch --quick
```

`--quick` skips the HITL save review so you don't have to babysit the run.
All 5 traces will share one `session_id` (e.g. `hw12-session-a1b2c3d4`) and
one `user_id` (default `dmytro@uni`, override with `--user-id`).

Alternatively, interactive REPL:

```powershell
python main.py
```

```powershell
# Or a single topic:
python main.py --topic "How does RAG compare to long-context LLMs?"
```

After the run, wait ~1–2 minutes for the evaluators to finish, then
take the **4 screenshots** described in `screenshots/README.md`.

## 6. Project layout

```
homework-lesson-12/
├── README.md                  ← you are here
├── requirements.txt
├── .env.example
├── .gitignore
├── config.py                  ← pydantic settings (incl. Langfuse keys)
├── langfuse_setup.py          ← Langfuse client, CallbackHandler, observe()
├── prompts.py                 ← prompt loader (Langfuse → text)
├── seed_prompts.py            ← one-shot prompt uploader
├── schemas.py                 ← ResearchPlan / CritiqueResult
├── tools.py                   ← @observe-instrumented LangChain tools
├── retriever.py / ingest.py   ← hybrid BM25 + FAISS retriever
├── agents/
│   ├── __init__.py
│   ├── planner.py             ← @observe + prompt from Langfuse
│   ├── research.py            ← @observe + prompt from Langfuse + tools
│   └── critic.py              ← @observe + prompt from Langfuse
├── supervisor.py              ← parent @observe trace, session/user/tag propagation
├── main.py                    ← CLI (interactive / batch / one-shot)
├── evaluators/                ← 3 ready-to-paste evaluator definitions
└── screenshots/               ← 4 screenshots go here (instructions in folder)
```

## 7. How the requirements are satisfied

### (1) Trace with full tree

- `supervisor.run()` is wrapped in `@observe(name="multi-agent-run")` →
  parent trace.
- Each agent method (`PlannerAgent.run`, `ResearchAgent.run`,
  `CriticAgent.run`) is wrapped in its own `@observe(name="agent.<role>")` →
  child span.
- Every agent invokes its inner LangChain agent with
  `config=langchain_config()`, which injects the singleton
  `CallbackHandler` — every LLM call and every tool call gets nested.
- Tools (`search_web`, `retrieve_local_context`, `save_report_raw`) also
  carry `@observe(as_type="span")` so even if a future tool is called
  outside a LangChain agent, it still threads correctly.

### (2) Session + User tracking

`supervisor.run()` immediately calls:

```python
update_trace(
    session_id=resolved_session,
    user_id=resolved_user,
    tags=["hw12", "multi-agent", "research", *extra_tags],
    metadata={...models / config snapshot...},
    input={"topic": topic},
)
```

The CLI passes a single `session_id` to every run in a batch, so the
**Sessions** tab in Langfuse shows the session and the **Users** tab shows
your `user_id`.

### (3) No hardcoded prompts

Run `git grep -n "system_prompt = " agents/` — every match is a call to
`get_prompt(...)`, not a string literal. The actual prompt texts live only
in two places: Langfuse (production source of truth) and `seed_prompts.py`
(the script that originally uploaded them).

### (4) LLM-as-a-Judge

Three evaluators with three different score types are documented in
`evaluators/`. They reference `{{input}}` (topic) and `{{output}}`
(JSON with `report_markdown` etc.) — both fields are explicitly populated
on the parent trace through `update_trace(input=..., output=...)`, which
ensures the judge sees real content.

### (5) Screenshots

See `screenshots/README.md` for exact filenames and what each shot must
show. Total: 4 files (with one optional bonus).

---

## Troubleshooting

- **`PromptNotConfiguredError`** — Langfuse env vars missing. Fill in `.env`
  and re-run.
- **`No prompt found with name "hw12/..."`** — you skipped step 2. Run
  `python seed_prompts.py`.
- **Evaluator scores never appear** — check
  **LLM-as-a-Judge → Evaluators**: status should be `Active` and "traces
  processed" should be ticking up. Make sure the evaluator's **Target**
  scope is set to `multi-agent-run` or "All traces", not a filtered subset
  that excludes your runs.
- **Trace tree is shallow** — ensure your `langchain` / `langgraph` /
  `langfuse` versions match `requirements.txt`. Old `langfuse<3.0` uses a
  different decorator import path.
- **PowerShell prints `OPENAI_API_KEY` warnings** — make sure `.env` is in
  `homework-lesson-12/` (the same folder as `config.py`), not in the
  parent repo.
