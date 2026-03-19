from dataclasses import dataclass, field


@dataclass(slots=True)
class PullRequestResult:
    success: bool
    title: str
    source_branch: str
    target_branch: str
    url: str = ""
    error: str = ""
    repo_url: str = ""
    data: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "success": self.success,
            "title": self.title,
            "source_branch": self.source_branch,
            "target_branch": self.target_branch,
            "url": self.url,
            "error": self.error,
            "repo_url": self.repo_url,
            "data": dict(self.data),
        }
