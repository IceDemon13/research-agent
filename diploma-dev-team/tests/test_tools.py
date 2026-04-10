from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import tools as tools_module
from agents.business_analyst import BusinessAnalystAgent
from agents.developer import DeveloperAgent
from agents.qa_engineer import QaEngineerAgent
from tests.helpers import (
    FakeExecutor,
    FakeDDGS,
    fake_hybrid_search,
    make_code_output,
    make_review,
    make_spec,
    reset_workspace,
    write_workspace_demo_files,
)
from tools import (
    get_tool_trace,
    knowledge_search,
    list_project_files,
    read_project_file,
    reset_tool_trace,
    run_python_checks,
    search_web,
)


def test_ba_uses_search_and_rag_tools():
    reset_tool_trace()
    original_ddgs = tools_module.DDGS
    original_hybrid_search = tools_module.hybrid_search
    original_trafilatura = tools_module.trafilatura
    tools_module.DDGS = FakeDDGS
    tools_module.hybrid_search = fake_hybrid_search
    tools_module.trafilatura = None

    try:
        def response_factory(payload):
            search_web.invoke({"query": "python login docs", "max_results": 1})
            knowledge_search.invoke({"query": "coding standards login", "top_k": 2})
            return {"structured_response": make_spec("Login Feature")}

        spec = BusinessAnalystAgent(agent_executor=FakeExecutor(response_factory)).run("Login feature request")
        trace = get_tool_trace()

        assert spec.title == "Login Feature"
        assert any(item["tool"] == "search_web" for item in trace)
        assert any(item["tool"] == "knowledge_search" for item in trace)
    finally:
        tools_module.DDGS = original_ddgs
        tools_module.hybrid_search = original_hybrid_search
        tools_module.trafilatura = original_trafilatura


def test_developer_creates_files_via_bounded_tools():
    reset_workspace()
    reset_tool_trace()
    executor = FakeExecutor(lambda payload: {"structured_response": make_code_output("CSV Export")})

    code_output = DeveloperAgent(agent_executor=executor).run(make_spec("CSV Export"))
    trace = get_tool_trace()

    assert code_output.files_created
    assert any(item["tool"] == "write_project_file" for item in trace)
    assert any(item["tool"] == "run_python_checks" for item in trace)
    assert any(item["tool"] == "list_project_files" for item in trace)


def test_qa_reads_and_checks_workspace_files():
    reset_workspace()
    reset_tool_trace()
    write_workspace_demo_files()

    def response_factory(payload):
        read_project_file.invoke({"path": "src/main.py"})
        list_project_files.invoke({})
        run_python_checks.invoke({"code": "def ok():\n    return True\n"})
        return {"structured_response": make_review("APPROVED", score=0.82)}

    review = QaEngineerAgent(agent_executor=FakeExecutor(response_factory)).run(
        make_spec("Read Workspace"),
        make_code_output("Read Workspace"),
    )
    trace = get_tool_trace()

    assert review.verdict == "APPROVED"
    assert any(item["tool"] == "read_project_file" for item in trace)
    assert any(item["tool"] == "list_project_files" for item in trace)
    assert any(item["tool"] == "run_python_checks" for item in trace)
