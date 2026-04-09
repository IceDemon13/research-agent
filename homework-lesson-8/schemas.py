from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field


class ResearchPlan(BaseModel):
    topic: str = Field(description="Research topic provided by the user.")
    objective: str = Field(description="Primary objective of the research task.")
    key_questions: list[str] = Field(
        default_factory=list,
        description="Questions that must be answered to satisfy the task.",
    )
    search_queries: list[str] = Field(
        default_factory=list,
        description="Concrete search queries the researcher should use.",
    )
    report_outline: list[str] = Field(
        default_factory=list,
        description="Expected markdown sections for the final report.",
    )
    success_criteria: list[str] = Field(
        default_factory=list,
        description="Conditions that make the report good enough to approve.",
    )


class CritiqueResult(BaseModel):
    verdict: Literal["approved", "revise"] = Field(
        description="Whether the report can be accepted or needs revision."
    )
    summary: str = Field(description="Short explanation of the verdict.")
    strengths: list[str] = Field(
        default_factory=list,
        description="Strong parts of the current report.",
    )
    issues: list[str] = Field(
        default_factory=list,
        description="Problems, omissions, or unclear claims that need attention.",
    )
    revision_instructions: list[str] = Field(
        default_factory=list,
        description="Concrete instructions for the next revision round.",
    )
