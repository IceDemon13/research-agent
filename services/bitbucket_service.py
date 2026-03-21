import json
import re
from urllib import error, parse, request

from config import settings
from contracts.pull_request_contract import PullRequestResult
from logger_utils import log_line
from services.scm_service import build_bitbucket_basic_auth_header


BITBUCKET_REPO_PATTERNS = (
    re.compile(r"^https?://bitbucket\.org/(?P<workspace>[^/]+)/(?P<repo>[^/.]+?)(?:\.git)?/?$"),
    re.compile(r"^git@bitbucket\.org:(?P<workspace>[^/]+)/(?P<repo>[^/.]+?)(?:\.git)?$"),
)


class BitbucketService:
    def create_pull_request(
        self,
        repo_url: str,
        source_branch: str,
        target_branch: str,
        title: str,
        description: str,
    ) -> PullRequestResult:
        resolved_repo_url = str(repo_url or "").strip()
        resolved_title = str(title or "").strip()
        resolved_source_branch = str(source_branch or "").strip()
        resolved_target_branch = str(target_branch or "").strip()
        resolved_description = str(description or "").strip()

        if not resolved_repo_url:
            return PullRequestResult(
                success=False,
                title=resolved_title,
                source_branch=resolved_source_branch,
                target_branch=resolved_target_branch,
                error="Repository URL is required.",
            )

        repo_details = self._parse_repo_url(resolved_repo_url)
        if repo_details is None:
            return PullRequestResult(
                success=False,
                title=resolved_title,
                source_branch=resolved_source_branch,
                target_branch=resolved_target_branch,
                repo_url=resolved_repo_url,
                error="Unsupported Bitbucket repository URL.",
            )

        auth_header = self._build_auth_header()
        if not auth_header:
            return PullRequestResult(
                success=False,
                title=resolved_title,
                source_branch=resolved_source_branch,
                target_branch=resolved_target_branch,
                repo_url=resolved_repo_url,
                error="Bitbucket credentials are not configured.",
            )

        api_base_url = str(settings.runtime.bitbucket_api_base_url or "").rstrip("/")
        pr_url = (
            f"{api_base_url}/repositories/"
            f"{parse.quote(repo_details['workspace'])}/"
            f"{parse.quote(repo_details['repo_slug'])}/pullrequests"
        )
        payload = {
            "title": resolved_title,
            "description": resolved_description,
            "source": {"branch": {"name": resolved_source_branch}},
            "destination": {"branch": {"name": resolved_target_branch}},
            "close_source_branch": False,
        }
        request_body = json.dumps(payload).encode("utf-8")
        http_request = request.Request(
            pr_url,
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
        except error.HTTPError as exc:
            response_body = exc.read().decode("utf-8", errors="replace")
            log_line(
                "BITBUCKET SERVICE FAILURE: "
                f"status={exc.code} repo={resolved_repo_url} error={response_body}"
            )
            return PullRequestResult(
                success=False,
                title=resolved_title,
                source_branch=resolved_source_branch,
                target_branch=resolved_target_branch,
                repo_url=resolved_repo_url,
                error=response_body or f"Bitbucket API returned HTTP {exc.code}.",
            )
        except error.URLError as exc:
            log_line(
                "BITBUCKET SERVICE FAILURE: "
                f"repo={resolved_repo_url} error={exc}"
            )
            return PullRequestResult(
                success=False,
                title=resolved_title,
                source_branch=resolved_source_branch,
                target_branch=resolved_target_branch,
                repo_url=resolved_repo_url,
                error=str(exc),
            )

        try:
            payload_data = json.loads(response_body)
        except json.JSONDecodeError:
            payload_data = {}

        pr_link = ""
        if isinstance(payload_data, dict):
            links = payload_data.get("links", {})
            html_link = links.get("html", {}) if isinstance(links, dict) else {}
            pr_link = str(html_link.get("href", "") or payload_data.get("url", "") or "").strip()

        log_line(
            "BITBUCKET SERVICE SUCCESS: "
            f"repo={resolved_repo_url} source={resolved_source_branch} target={resolved_target_branch}"
        )
        return PullRequestResult(
            success=True,
            title=resolved_title,
            source_branch=resolved_source_branch,
            target_branch=resolved_target_branch,
            url=pr_link,
            repo_url=resolved_repo_url,
            data=payload_data if isinstance(payload_data, dict) else {},
        )

    @staticmethod
    def _parse_repo_url(repo_url: str) -> dict[str, str] | None:
        for pattern in BITBUCKET_REPO_PATTERNS:
            match = pattern.match(repo_url)
            if match:
                return {
                    "workspace": str(match.group("workspace") or "").strip(),
                    "repo_slug": str(match.group("repo") or "").strip(),
                }
        return None

    @staticmethod
    def _build_auth_header() -> str:
        return build_bitbucket_basic_auth_header()
