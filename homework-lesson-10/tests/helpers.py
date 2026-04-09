from __future__ import annotations

import json
from pathlib import Path
from types import SimpleNamespace

from schemas import CritiqueResult, ResearchPlan
from tools import save_report_raw


class FakeExecutor:
    def __init__(self, response_factory):
        self.response_factory = response_factory
        self.calls: list[dict] = []

    def invoke(self, payload):
        self.calls.append(payload)
        return self.response_factory(payload)


class FakeSaveAgent:
    def __init__(self):
        self.saved_title = ""
        self.saved_content = ""

    def invoke(self, payload, config=None, version=None):
        if isinstance(payload, dict):
            content = payload["messages"][0]["content"]
            title_marker = "Title: "
            markdown_marker = "Markdown:\n"
            title_start = content.find(title_marker) + len(title_marker)
            markdown_start = content.find(markdown_marker)
            self.saved_title = content[title_start:markdown_start].strip()
            self.saved_content = content[markdown_start + len(markdown_marker):]
            interrupt = SimpleNamespace(
                value={"action_requests": [{"description": "Approve or reject save_report"}]}
            )
            return SimpleNamespace(interrupts=(interrupt,))

        resume = getattr(payload, "resume", {}) or {}
        decisions = resume.get("decisions", [])
        decision = decisions[0] if decisions else {}
        if decision.get("type") == "approve":
            saved_path = save_report_raw(self.saved_title, self.saved_content)
            return {"saved_path": saved_path}
        if decision.get("type") == "edit":
            args = decision["edited_action"]["args"]
            saved_path = save_report_raw(args["title"], args["content"])
            return {"saved_path": saved_path}
        return {"status": "rejected"}


def make_plan(topic: str = "AI research automation") -> ResearchPlan:
    return ResearchPlan(
        topic=topic,
        objective="Produce a concise research brief.",
        key_questions=["What problem are we studying?", "Which sources are credible?"],
        search_queries=["AI research automation overview", "research agent evaluation"],
        report_outline=["Executive Summary", "Findings", "Sources"],
        success_criteria=["Clear scope", "Grounded citations"],
    )


def make_critique(verdict: str = "revise") -> CritiqueResult:
    return CritiqueResult(
        verdict=verdict,
        summary="Needs sharper citations and clearer scope.",
        strengths=["Clear structure"],
        issues=["Missing citations"],
        revision_instructions=["Add source links", "Tighten the scope"],
    )


def load_dataset() -> list[dict]:
    dataset_path = Path(__file__).resolve().parent / "golden_dataset.json"
    return json.loads(dataset_path.read_text(encoding="utf-8"))
