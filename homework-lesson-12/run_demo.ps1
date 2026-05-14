# run_demo.ps1 — one-click setup for HW12
#
# Usage (from PowerShell, inside homework-lesson-12 folder):
#
#     .\run_demo.ps1
#
# What it does:
#   1. Creates / activates a local .venv
#   2. Installs requirements.txt (quiet)
#   3. Sanity-checks Langfuse + OpenAI keys (fails fast if wrong)
#   4. Uploads the 4 system prompts to Langfuse Prompt Management
#   5. Runs the batch of 5 research topics, all under one session_id
#
# After it finishes:
#   • Open Langfuse UI -> Prompts to verify (screenshot #3)
#   • Set up 2-3 evaluators from evaluators/*.md (one-time)
#   • Run `python main.py --batch --quick` again so the evaluators
#     score the new traces (wait 1-2 minutes after run finishes)
#   • Take screenshots per screenshots/README.md

$ErrorActionPreference = "Stop"

function Section($title) {
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host $title -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Cyan
}

# --- 0. Sanity checks ----------------------------------------------------
Section "0. Pre-flight"
if (-not (Test-Path ".env")) {
    Write-Host "ERROR: .env not found. Copy .env.example to .env and fill it." -ForegroundColor Red
    exit 1
}
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: 'python' not on PATH. Install Python 3.11+." -ForegroundColor Red
    exit 1
}
Write-Host ".env present" -ForegroundColor Green
python --version

# --- 1. venv -------------------------------------------------------------
Section "1. Virtualenv"
if (-not (Test-Path ".venv")) {
    Write-Host "Creating .venv ..."
    python -m venv .venv
}
. .\.venv\Scripts\Activate.ps1
Write-Host "Activated: $($env:VIRTUAL_ENV)" -ForegroundColor Green

# --- 2. Dependencies -----------------------------------------------------
Section "2. Installing requirements"
python -m pip install --quiet --upgrade pip
pip install --quiet -r requirements.txt
Write-Host "Done." -ForegroundColor Green

# --- 3. Auth check -------------------------------------------------------
Section "3. Auth check (Langfuse + OpenAI)"
python -c @"
from dotenv import load_dotenv
load_dotenv('.env')
import os
from langfuse import get_client
client = get_client()
print('Langfuse host    :', os.environ.get('LANGFUSE_BASE_URL') or os.environ.get('LANGFUSE_HOST'))
print('Langfuse auth_ok :', client.auth_check())
# OpenAI smoke test
from openai import OpenAI
api_key = os.environ.get('OPENAI_API_KEY')
if not api_key:
    raise SystemExit('OPENAI_API_KEY missing')
OpenAI(api_key=api_key).models.list()  # raises if key invalid
print('OpenAI auth_ok   : True')
"@
if ($LASTEXITCODE -ne 0) {
    Write-Host "Auth check FAILED. Fix .env and retry." -ForegroundColor Red
    exit 1
}

# --- 4. Seed prompts -----------------------------------------------------
Section "4. Uploading prompts to Langfuse Prompt Management"
python seed_prompts.py

# --- 5. Generate traces --------------------------------------------------
Section "5. Running batch of 5 research topics (creates 5 traces in one session)"
python main.py --batch --quick

# --- Done ----------------------------------------------------------------
Section "Done"
Write-Host @"
Next steps:
  1) Open Langfuse UI -> Prompts and confirm 4 prompts (screenshot #3).
  2) Open Langfuse UI -> Tracing -> Traces, open the newest 'multi-agent-run'
     trace. Confirm the tree shows agent.planner / agent.research / agent.critic
     and nested tool.search_web / tool.retrieve_local_context calls (screenshot #1).
  3) Open Langfuse UI -> Sessions, open today's hw12-session-* (screenshot #2).
  4) (One-time) Go to LLM-as-a-Judge -> Evaluators and create 2-3 from
     evaluators/*.md. Sample rate = 1.0, target = 'multi-agent-run' (or 'New traces').
  5) Run `python main.py --batch --quick` ONCE MORE so the new evaluators
     score the freshly created traces.
  6) Wait ~2 minutes. Open any new trace -> Scores tab (screenshot #4).
  7) Save the 4 screenshots into screenshots/ per screenshots/README.md.
"@ -ForegroundColor Green
