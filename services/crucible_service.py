from __future__ import annotations

import base64
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib import error, request

from config import settings
from contracts.crucible_review_contract import CrucibleReviewResult
from logger_utils import log_line


CRUCIBLE_REVIEW_REGISTRY_VERSION = 1


class CrucibleService:
    def __init__(self, storage_path: str | Path | None = None) -> None:
        resolved_storage_path = Path(
            storage_path or settings.runtime.crucible_review_registry_path
        )
        self._storage_path = resolved_storage_path

    def create_review(
        self,
        repo: str,
        branch: str,
        title: str,
        description: str,
        reviewers: list[str],
    ) -> CrucibleReviewResult:
        resolved_repo = str(repo or "").strip()
        resolved_branch = str(branch or "").strip()
        resolved_title = str(title or "").strip()
        resolved_description = str(description or "").strip()
        resolved_reviewers = [
            reviewer.strip()
            for reviewer in list(reviewers or [])
            if str(reviewer or "").strip()
        ]

        if not resolved_repo:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Repository name is required for Crucible review creation.",
            )
        if not resolved_branch:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Branch name is required for Crucible review creation.",
            )
        if not resolved_title:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Review title is required.",
            )

        project_key = str(settings.runtime.crucible_project_key or "").strip()
        base_url = str(settings.runtime.crucible_base_url or "").rstrip("/")
        if not project_key:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Crucible project key is not configured.",
            )
        if not base_url:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Crucible base URL is not configured.",
            )

        auth_header = self._build_auth_header()
        if not auth_header:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error="Crucible credentials are not configured.",
            )

        existing_entry = self._find_existing_review(resolved_repo, resolved_branch)
        if existing_entry is not None:
            return CrucibleReviewResult(
                success=True,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                url=str(existing_entry.get("url", "") or "").strip(),
                review_id=str(existing_entry.get("review_id", "") or "").strip(),
                duplicate=True,
                warnings=["Crucible review already exists for this repo and branch."],
                data=dict(existing_entry),
            )

        create_url = f"{base_url}/rest-service/reviews-v1"
        create_payload = {
            "reviewData": {
                "projectKey": project_key,
                "name": resolved_title,
                "description": resolved_description,
                "summary": f"{resolved_repo}:{resolved_branch}",
                "type": "REVIEW",
                "allowReviewersToJoin": True,
            }
        }
        create_result = self._post_json(
            create_url,
            create_payload,
            auth_header=auth_header,
            operation="create_review",
        )
        if not create_result["success"]:
            return CrucibleReviewResult(
                success=False,
                title=resolved_title,
                repo=resolved_repo,
                branch=resolved_branch,
                reviewers=resolved_reviewers,
                error=str(create_result["error"] or "Failed to create Crucible review."),
            )

        payload_data = create_result["data"] if isinstance(create_result["data"], dict) else {}
        location_url = str(create_result["location"] or "").strip()
        review_id = self._extract_review_id(location_url, payload_data)
        review_url = location_url or self._build_review_url(base_url, review_id)
        warnings: list[str] = []

        if review_id and resolved_reviewers:
            reviewer_payload = {
                "reviewer": [
                    {"userName": reviewer}
                    for reviewer in resolved_reviewers
                ]
            }
            reviewers_url = f"{base_url}/rest-service/reviews-v1/{review_id}/reviewers"
            reviewer_result = self._post_json(
                reviewers_url,
                reviewer_payload,
                auth_header=auth_header,
                operation="add_reviewers",
            )
            if not reviewer_result["success"]:
                warnings.append(
                    str(reviewer_result["error"] or "Failed to add Crucible reviewers.")
                )
        elif resolved_reviewers and not review_id:
            warnings.append("Crucible review was created, but reviewer assignment was skipped because review_id was unavailable.")

        review_entry = {
            "repo": resolved_repo,
            "branch": resolved_branch,
            "title": resolved_title,
            "url": review_url,
            "review_id": review_id,
            "created_at": datetime.now(timezone.utc).isoformat(),
        }
        self._save_review_entry(review_entry)
        log_line(
            "CRUCIBLE SERVICE SUCCESS: "
            f"repo={resolved_repo} branch={resolved_branch} review_id={review_id or 'unknown'}"
        )
        return CrucibleReviewResult(
            success=True,
            title=resolved_title,
            repo=resolved_repo,
            branch=resolved_branch,
            reviewers=resolved_reviewers,
            url=review_url,
            review_id=review_id,
            warnings=warnings,
            data=payload_data,
        )

    def _post_json(
        self,
        url: str,
        payload: dict,
        *,
        auth_header: str,
        operation: str,
    ) -> dict:
        request_body = json.dumps(payload).encode("utf-8")
        http_request = request.Request(
            url,
            data=request_body,
            headers={
                "Authorization": auth_header,
                "Content-Type": "application/json",
                "Accept": "application/json",
            },
            method="POST",
        )

        try:
            with request.urlopen(http_request, timeout=20) as response:
                response_body = response.read().decode("utf-8", errors="replace")
                location_url = str(response.headers.get("Location", "") or "").strip()
        except error.HTTPError as exc:
            response_body = exc.read().decode("utf-8", errors="replace")
            log_line(
                "CRUCIBLE SERVICE FAILURE: "
                f"operation={operation} status={exc.code} url={url} error={response_body}"
            )
            return {
                "success": False,
                "error": response_body or f"Crucible API returned HTTP {exc.code}.",
                "data": {},
                "location": "",
            }
        except error.URLError as exc:
            log_line(
                "CRUCIBLE SERVICE FAILURE: "
                f"operation={operation} url={url} error={exc}"
            )
            return {
                "success": False,
                "error": str(exc),
                "data": {},
                "location": "",
            }

        try:
            payload_data = json.loads(response_body) if response_body else {}
        except json.JSONDecodeError:
            payload_data = {}
        return {
            "success": True,
            "error": "",
            "data": payload_data if isinstance(payload_data, dict) else {},
            "location": location_url,
        }

    @staticmethod
    def _extract_review_id(location_url: str, payload_data: dict) -> str:
        if location_url:
            cleaned = location_url.rstrip("/")
            if cleaned:
                return cleaned.split("/")[-1]
        perma_id = payload_data.get("permaId", {}) if isinstance(payload_data, dict) else {}
        return str(perma_id.get("id", "") or "").strip()

    @staticmethod
    def _build_review_url(base_url: str, review_id: str) -> str:
        cleaned_review_id = str(review_id or "").strip()
        if not cleaned_review_id:
            return ""
        return f"{base_url}/cru/{cleaned_review_id}"

    @staticmethod
    def _build_auth_header() -> str:
        username = str(settings.runtime.crucible_username or "").strip()
        password = str(settings.runtime.crucible_password or "").strip()
        api_token = str(settings.runtime.crucible_api_token or "").strip()

        if username and password:
            token = base64.b64encode(f"{username}:{password}".encode("utf-8")).decode("ascii")
            return f"Basic {token}"
        if api_token:
            return f"Bearer {api_token}"
        return ""

    def _find_existing_review(self, repo: str, branch: str) -> dict | None:
        state = self._load_registry()
        for item in list(state.get("reviews", []) or []):
            if (
                str(item.get("repo", "") or "").strip() == repo
                and str(item.get("branch", "") or "").strip() == branch
            ):
                return dict(item)
        return None

    def _save_review_entry(self, entry: dict) -> None:
        state = self._load_registry()
        reviews = [
            item
            for item in list(state.get("reviews", []) or [])
            if not (
                str(item.get("repo", "") or "").strip() == str(entry.get("repo", "") or "").strip()
                and str(item.get("branch", "") or "").strip() == str(entry.get("branch", "") or "").strip()
            )
        ]
        reviews.append(dict(entry))
        self._storage_path.parent.mkdir(parents=True, exist_ok=True)
        self._storage_path.write_text(
            json.dumps(
                {
                    "version": CRUCIBLE_REVIEW_REGISTRY_VERSION,
                    "reviews": reviews,
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

    def _load_registry(self) -> dict:
        if not self._storage_path.exists():
            return {
                "version": CRUCIBLE_REVIEW_REGISTRY_VERSION,
                "reviews": [],
            }
        try:
            payload = json.loads(self._storage_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {
                "version": CRUCIBLE_REVIEW_REGISTRY_VERSION,
                "reviews": [],
            }
        if not isinstance(payload, dict):
            return {
                "version": CRUCIBLE_REVIEW_REGISTRY_VERSION,
                "reviews": [],
            }
        reviews = payload.get("reviews", [])
        return {
            "version": payload.get("version", CRUCIBLE_REVIEW_REGISTRY_VERSION),
            "reviews": reviews if isinstance(reviews, list) else [],
        }
