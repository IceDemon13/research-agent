from dataclasses import dataclass, field


@dataclass(slots=True)
class CrucibleReviewResult:
    success: bool
    title: str
    repo: str
    branch: str
    reviewers: list[str] = field(default_factory=list)
    url: str = ""
    review_id: str = ""
    duplicate: bool = False
    error: str = ""
    warnings: list[str] = field(default_factory=list)
    data: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "success": self.success,
            "title": self.title,
            "repo": self.repo,
            "branch": self.branch,
            "reviewers": list(self.reviewers),
            "url": self.url,
            "review_id": self.review_id,
            "duplicate": self.duplicate,
            "error": self.error,
            "warnings": list(self.warnings),
            "data": dict(self.data),
        }
