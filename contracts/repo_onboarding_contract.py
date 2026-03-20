from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class RepoOnboardingResult:
    repo_id: str
    remote_url: str
    local_path: str
    status: str
    message: str

    def to_dict(self) -> dict[str, str]:
        return {
            "repo_id": self.repo_id,
            "remote_url": self.remote_url,
            "local_path": self.local_path,
            "status": self.status,
            "message": self.message,
        }
