from __future__ import annotations

import json
import shutil
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from config import settings
from schemas import CodeOutput, ReviewOutput, SpecOutput
from tools import write_project_file


class FakeExecutor:
    def __init__(self, response_factory):
        self.response_factory = response_factory
        self.calls: list[dict] = []

    def invoke(self, payload):
        self.calls.append(payload)
        return self.response_factory(payload)


class FakeSearchResult:
    def __init__(self, text: str):
        self.text = text

    def to_dict(self) -> dict:
        return {"source": "local-note.md", "content": self.text, "score": 0.91}


class FakeDDGS:
    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, tb):
        return False

    def text(self, query, max_results=5):
        return [
            {
                "title": "Stub result",
                "href": "https://example.com/dev-doc",
                "body": f"Stub search result for {query}",
            }
        ]


def fake_hybrid_search(query: str, top_k: int = 5):
    return [FakeSearchResult(f"Knowledge hit for {query}")] * min(top_k, 2)


def reset_workspace() -> None:
    workspace = settings.workspace_dir
    workspace.mkdir(parents=True, exist_ok=True)
    for path in list(workspace.iterdir()):
        if path.name == ".gitkeep":
            continue
        if path.is_dir():
            shutil.rmtree(path)
        else:
            path.unlink()


def reset_output() -> None:
    output = settings.output_dir
    output.mkdir(parents=True, exist_ok=True)
    for path in list(output.iterdir()):
        if path.name == ".gitkeep":
            continue
        if path.is_dir():
            shutil.rmtree(path)
        else:
            path.unlink()


def make_spec(title: str = "Build Login Form", complexity: str = "medium") -> SpecOutput:
    return SpecOutput(
        title=title,
        requirements=[
            "Provide a simple but working Python entrypoint.",
            "Represent the requested feature in a lightweight demo format.",
        ],
        acceptance_criteria=[
            "Generated project contains runnable Python code.",
            "Workspace files are created for demo purposes.",
        ],
        estimated_complexity=complexity,
    )


def make_code_output(title: str = "Build Login Form") -> CodeOutput:
    return CodeOutput(
        source_code=(
            "def main() -> str:\n"
            f"    return {json.dumps(title, ensure_ascii=False)}\n"
        ),
        description=f"Implements a demo scaffold for {title}.",
        files_created=["src/main.py", "tests/test_main.py", "requirements.txt"],
    )


def make_review(verdict: str = "APPROVED", score: float = 0.9) -> ReviewOutput:
    issues = [] if verdict == "APPROVED" else ["Main flow does not yet cover all acceptance criteria."]
    suggestions = (
        ["Keep the project structure and proceed to final report."]
        if verdict == "APPROVED"
        else ["Add one more acceptance-criteria-oriented assertion.", "Clarify the README usage note."]
    )
    return ReviewOutput(
        verdict=verdict,
        issues=issues,
        suggestions=suggestions,
        score=score,
    )


def write_workspace_demo_files() -> None:
    write_project_file.invoke({"path": "src/main.py", "content": "def main():\n    return 'ok'\n"})
    write_project_file.invoke({"path": "tests/test_main.py", "content": "from src.main import main\n"})


def render_delivery_summary(spec: SpecOutput, code_output: CodeOutput, review: ReviewOutput) -> str:
    return (
        f"Title: {spec.title}\n"
        f"Requirements: {', '.join(spec.requirements)}\n"
        f"Acceptance Criteria: {', '.join(spec.acceptance_criteria)}\n"
        f"Description: {code_output.description}\n"
        f"Files: {', '.join(code_output.files_created)}\n"
        f"QA Verdict: {review.verdict}\n"
        f"QA Suggestions: {', '.join(review.suggestions)}"
    )
