from __future__ import annotations

import sys
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import tools as tools_module
from agents.planner import PlannerAgent
from agents.research import ResearchAgent
from schemas import CritiqueResult, ResearchPlan
from supervisor import HomeworkSupervisor, SupervisorResult
from tests.helpers import FakeExecutor, FakeSaveAgent, make_plan
from tools import get_tool_trace, reset_tool_trace, retrieve_local_context, save_report, search_web


class FakeDDGS:
    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, tb):
        return False

    def text(self, query, max_results=5):
        return [{"title": "Stub", "href": "https://example.com", "body": f"Stub result for {query}"}]


def test_planner_tool_trace_records_search_usage():
    reset_tool_trace()
    original_ddgs = tools_module.DDGS
    tools_module.DDGS = FakeDDGS

    try:
        def response_factory(payload):
            search_web.invoke({"query": "planner query", "max_results": 1})
            return {"structured_response": make_plan("Planner uses tools")}

        executor = FakeExecutor(response_factory)
        plan = PlannerAgent(agent_executor=executor).run("Planner uses tools")
        trace = get_tool_trace()

        assert isinstance(plan, ResearchPlan)
        assert any(item["tool"] == "search_web" for item in trace)
    finally:
        tools_module.DDGS = original_ddgs


def test_researcher_tool_trace_records_retrieval_usage():
    reset_tool_trace()
    original_ddgs = tools_module.DDGS
    tools_module.DDGS = FakeDDGS

    try:
        def response_factory(payload):
            retrieve_local_context.invoke({"query": "retrieval query", "top_k": 2})
            search_web.invoke({"query": "retrieval web query", "max_results": 1})
            report = "# Report\n\n## Sources\n- https://example.com\n- local_note.md"
            return {"messages": [type("Msg", (), {"content": report})()]}

        executor = FakeExecutor(response_factory)
        report = ResearchAgent(agent_executor=executor).run("Topic", make_plan("Topic"))
        trace = get_tool_trace()

        assert "## Sources" in report
        assert any(item["tool"] == "retrieve_local_context" for item in trace)
        assert any(item["tool"] == "search_web" for item in trace)
    finally:
        tools_module.DDGS = original_ddgs


def test_supervisor_approve_triggers_save_report():
    reset_tool_trace()
    fake_save_agent = FakeSaveAgent()
    supervisor = HomeworkSupervisor(
        planner=SimpleNamespace(),
        researcher=SimpleNamespace(),
        critic=SimpleNamespace(),
        save_agent=fake_save_agent,
    )
    result = SupervisorResult(
        topic="Supervisor tool test",
        plan=make_plan("Supervisor tool test"),
        report_markdown="# Final Report\n\nSaved after approval.",
        critique=CritiqueResult(
            verdict="approved",
            summary="Looks good.",
            strengths=["Grounded"],
            issues=[],
            revision_instructions=[],
        ),
        revision_count=0,
    )

    _, thread_id = supervisor.request_save(result)
    resumed = supervisor.resume_save(thread_id, {"type": "approve"})
    trace = get_tool_trace()

    assert "saved_path" in resumed
    assert any(item["tool"] == "save_report" for item in trace)
