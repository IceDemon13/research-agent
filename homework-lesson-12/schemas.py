"""Pydantic schemas shared by the multi-agent workflow."""
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
    verdict: Literal["approved", "revise"] = Field(
        description="Whether the report can be accepted or needs revision."
    )
    summary: str
    strengths: list[str] = Field(default_factory=list)
    issues: list[str] = Field(default_factory=list)
    revision_instructions: list[str] = Field(default_factory=list)
