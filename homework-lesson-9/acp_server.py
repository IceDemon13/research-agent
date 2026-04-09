from __future__ import annotations

import traceback

from fastapi import FastAPI
from fastapi.responses import JSONResponse
from uvicorn import run as uvicorn_run

from agents import CriticAgent, PlannerAgent, ResearchAgent
from config import settings
from schemas import (
    CriticRequest,
    CriticResponse,
    PlannerRequest,
    PlannerResponse,
    ResearchRequest,
    ResearchResponse,
)


def _validate_startup_config() -> None:
    missing: list[str] = []
    if not settings.openai_api_key:
        missing.append("OPENAI_API_KEY")
    if missing:
        message = f"[ACP] missing required environment values: {', '.join(missing)}"
        print(message)
        raise RuntimeError(message)
    print(
        "[ACP] config ready: "
        f"OPENAI_API_KEY={'set' if settings.openai_api_key else 'missing'}, "
        f"search_mcp_url={settings.search_mcp_url}, "
        f"report_mcp_url={settings.report_mcp_url}, "
        f"planner_model={settings.planner_model}"
    )


_validate_startup_config()

app = FastAPI(title="Homework Lesson 9 ACP Server")
planner_agent = PlannerAgent()
research_agent = ResearchAgent()
critic_agent = CriticAgent()


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/agents/planner", response_model=PlannerResponse)
async def planner_endpoint(request: PlannerRequest):
    print(f"[ACP] planner request: topic={request.topic}")
    try:
        plan = await planner_agent.run(request.topic)
        print("[ACP] planner success")
        return PlannerResponse(plan=plan)
    except Exception as exc:
        print(f"[ACP] planner exception: {type(exc).__name__}: {exc}")
        traceback.print_exc()
        return JSONResponse(
            status_code=500,
            content={
                "error": "planner_failed",
                "message": str(exc),
                "exception_type": type(exc).__name__,
            },
        )


@app.post("/agents/researcher", response_model=ResearchResponse)
async def researcher_endpoint(request: ResearchRequest):
    print(f"[ACP] researcher request: topic={request.topic}")
    try:
        report_markdown = await research_agent.run(
            request.topic,
            request.plan,
            critique_feedback=request.critique_feedback,
        )
        print("[ACP] researcher success")
        return ResearchResponse(report_markdown=report_markdown)
    except Exception as exc:
        print(f"[ACP] researcher exception: {type(exc).__name__}: {exc}")
        traceback.print_exc()
        return JSONResponse(
            status_code=500,
            content={
                "error": "researcher_failed",
                "message": str(exc),
                "exception_type": type(exc).__name__,
            },
        )


@app.post("/agents/critic", response_model=CriticResponse)
async def critic_endpoint(request: CriticRequest):
    print(f"[ACP] critic request: topic={request.topic}, revision_round={request.revision_round}")
    try:
        critique = await critic_agent.run(
            request.topic,
            request.plan,
            request.report_markdown,
            revision_round=request.revision_round,
        )
        print("[ACP] critic success")
        return CriticResponse(critique=critique)
    except Exception as exc:
        print(f"[ACP] critic exception: {type(exc).__name__}: {exc}")
        traceback.print_exc()
        return JSONResponse(
            status_code=500,
            content={
                "error": "critic_failed",
                "message": str(exc),
                "exception_type": type(exc).__name__,
            },
        )


if __name__ == "__main__":
    print("[ACP] boot")
    print(f"[ACP] starting on port {settings.acp_port}")
    uvicorn_run(app, host="127.0.0.1", port=settings.acp_port)
