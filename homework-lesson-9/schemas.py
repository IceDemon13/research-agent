from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field


class ResearchPlan(BaseModel):
    topic: str = Field(description="Research topic provided by the user.")
    objective: str = Field(description="Primary objective of the research task.")
    key_questions: list[str] = Field(default_factory=list)
    search_queries: list[str] = Field(default_factory=list)
    report_outline: list[str] = Field(default_factory=list)
    success_criteria: list[str] = Field(default_factory=list)


class CritiqueResult(BaseModel):
    verdict: Literal["APPROVED", "REVISE"] = Field(description="Approval or revision verdict.")
    summary: str
    strengths: list[str] = Field(default_factory=list)
    issues: list[str] = Field(default_factory=list)
    revision_instructions: list[str] = Field(default_factory=list)


class PlannerRequest(BaseModel):
    topic: str


class ResearchRequest(BaseModel):
    topic: str
    plan: ResearchPlan
    critique_feedback: list[str] = Field(default_factory=list)


class CriticRequest(BaseModel):
    topic: str
    plan: ResearchPlan
    report_markdown: str
    revision_round: int = 0


class PlannerResponse(BaseModel):
    plan: ResearchPlan


class ResearchResponse(BaseModel):
    report_markdown: str


class CriticResponse(BaseModel):
    critique: CritiqueResult
