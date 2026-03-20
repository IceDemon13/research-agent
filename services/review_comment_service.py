from __future__ import annotations

import re

from contracts.diff_contract import DiffResult
from contracts.review_comment_contract import AIReviewComment


class ReviewCommentService:
    def generate_comments(self, diff_result: DiffResult | None) -> list[AIReviewComment]:
        if diff_result is None:
            return []
        comments: list[AIReviewComment] = []
        for diff_file in list(diff_result.files):
            diff_text = str(diff_file.diff or "")
            if not diff_text.strip():
                continue
            comments.extend(
                self._comments_for_file(
                    file_path=str(diff_file.relative_path or "").strip(),
                    diff_text=diff_text,
                )
            )
        return comments

    def _comments_for_file(self, *, file_path: str, diff_text: str) -> list[AIReviewComment]:
        matches: list[AIReviewComment] = []
        for line_hint, line_text in self._added_lines(diff_text):
            stripped = line_text.strip()
            lowered = stripped.lower()
            chunk_hint = stripped[:160]

            if re.match(r"except\s*:\s*$", stripped):
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="risk",
                        title="Bare exception handler added",
                        comment="A bare `except:` can hide unexpected failures and make debugging harder.",
                        suggested_check="Confirm the exception type is intentionally broad and log the failure path.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if "shell=true" in lowered:
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="risk",
                        title="Shell execution introduced",
                        comment="This change enables shell execution, which can widen command-injection risk.",
                        suggested_check="Verify all command input is trusted or properly escaped before release.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if "pdb.set_trace" in lowered or "breakpoint()" in lowered:
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="warning",
                        title="Debug breakpoint left in code",
                        comment="A breakpoint/debug hook was added in changed code.",
                        suggested_check="Confirm this is intended for production and remove it if it is only for local debugging.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if "todo" in lowered or "fixme" in lowered:
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="warning",
                        title="Follow-up marker in changed code",
                        comment="This diff introduces a TODO/FIXME marker in changed code.",
                        suggested_check="Check whether the implementation is intentionally incomplete or needs a tracked follow-up task.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if re.search(r"\bpass\b", stripped):
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="warning",
                        title="No-op statement added",
                        comment="A `pass` statement was introduced in changed code, which can leave behavior incomplete.",
                        suggested_check="Verify the new branch is intentionally a no-op and covered by tests.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if ("requests.get(" in lowered or "requests.post(" in lowered or "requests.request(" in lowered) and "timeout=" not in lowered:
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="warning",
                        title="HTTP call without timeout",
                        comment="A new outbound HTTP request was added without an explicit timeout on the same line.",
                        suggested_check="Confirm the call path has an intentional timeout to avoid hanging validation or runtime flows.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )
                continue

            if re.search(r"\bprint\s*\(", stripped):
                matches.append(
                    AIReviewComment(
                        file_path=file_path,
                        severity="info",
                        title="Direct console output added",
                        comment="A new `print(...)` call was introduced in changed code.",
                        suggested_check="Confirm this is expected user-facing output and not leftover debugging noise.",
                        line_hint=line_hint,
                        chunk_hint=chunk_hint,
                    )
                )

        deduped: list[AIReviewComment] = []
        seen: set[tuple[str, str, str]] = set()
        for item in matches:
            key = (item.file_path, item.title, item.line_hint)
            if key in seen:
                continue
            seen.add(key)
            deduped.append(item)
        return deduped

    @staticmethod
    def _added_lines(diff_text: str) -> list[tuple[str, str]]:
        added: list[tuple[str, str]] = []
        new_line_number = 0
        for raw_line in str(diff_text or "").splitlines():
            if raw_line.startswith("@@"):
                match = re.search(r"\+(\d+)", raw_line)
                if match:
                    new_line_number = int(match.group(1))
                continue
            if raw_line.startswith("+++") or raw_line.startswith("---") or raw_line.startswith("diff --git"):
                continue
            if raw_line.startswith("+"):
                added.append((str(new_line_number or ""), raw_line[1:]))
                new_line_number += 1
                continue
            if raw_line.startswith(" "):
                new_line_number += 1
                continue
            if raw_line.startswith("-"):
                continue
        return added
