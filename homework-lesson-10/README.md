# Homework Lesson 10

Isolated multi-agent research assistant with a pragmatic DeepEval baseline.

## Install

```powershell
pip install -r homework-lesson-10/requirements.txt
```

## Build Local Retrieval Index

```powershell
python homework-lesson-10/ingest.py
```

## Run DeepEval Suite

```powershell
deepeval test run homework-lesson-10/tests/
```

## Notes About The Test Suite

- `test_tools.py` is baseline and stub-assisted. It checks tool traces and save behavior without depending on live model calls.
- `test_planner.py`, `test_researcher.py`, and `test_critic.py` mix deterministic baseline checks with optional DeepEval-backed quality checks.
- `test_e2e.py` evaluates the full golden dataset with at least two metrics. It is designed as a pragmatic baseline and can fall back to skips when `deepeval` or a live judge model is unavailable.
- The custom business metric is a `GEval` focused on multi-agent quality, such as plan decomposition or citation discipline.
- The suite is structured so imports still work even if `deepeval` is not installed yet in the current environment.
