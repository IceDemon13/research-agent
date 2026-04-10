from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field


class SpecOutput(BaseModel):
    title: str
    requirements: list[str] = Field(default_factory=list)
    acceptance_criteria: list[str] = Field(default_factory=list)
    estimated_complexity: Literal["simple", "medium", "complex"]


class CodeOutput(BaseModel):
    source_code: str
    description: str
    files_created: list[str] = Field(default_factory=list)


class ReviewOutput(BaseModel):
    verdict: Literal["APPROVED", "REVISION_NEEDED"]
    issues: list[str] = Field(default_factory=list)
    suggestions: list[str] = Field(default_factory=list)
    score: float
