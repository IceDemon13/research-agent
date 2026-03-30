from dataclasses import dataclass, field


@dataclass(slots=True)
class WorkflowRunLink:
    run_id: str = ""
    status: str = ""
    run_detail_url: str = ""

    def to_dict(self) -> dict:
        return {
            "run_id": self.run_id,
            "status": self.status,
            "run_detail_url": self.run_detail_url,
        }


@dataclass(slots=True)
class FileChangeAction:
    file: str = ""
    action: str = ""
    description: str = ""

    def to_dict(self) -> dict:
        return {
            "file": self.file,
            "action": self.action,
            "description": self.description,
        }


@dataclass(slots=True)
class SelectionCandidate:
    name: str = ""
    confidence: float = 0.0
    reason: str = ""

    def to_dict(self) -> dict:
        return {
            "name": self.name,
            "confidence": float(self.confidence),
            "reason": self.reason,
        }


@dataclass(slots=True)
class AreaSuggestion:
    area: str = ""
    confidence: float = 0.0
    reason: str = ""

    def to_dict(self) -> dict:
        return {
            "area": self.area,
            "confidence": float(self.confidence),
            "reason": self.reason,
        }


@dataclass(slots=True)
class RequiredFix:
    file: str = ""
    what_to_fix: str = ""
    exact_action: str = ""
    why: str = ""

    def to_dict(self) -> dict:
        return {
            "file": self.file,
            "what_to_fix": self.what_to_fix,
            "exact_action": self.exact_action,
            "why": self.why,
        }


@dataclass(slots=True)
class ReviewIssue:
    file: str = ""
    issue: str = ""
    severity: str = ""
    impact: str = ""
    why: str = ""
    evidence_type: str = ""
    evidence_source: str = ""
    evidence_snippet: str = ""
    evidence_line: str = ""
    evidence_confidence: float = 0.0

    def to_dict(self) -> dict:
        return {
            "file": self.file,
            "issue": self.issue,
            "severity": self.severity,
            "impact": self.impact,
            "why": self.why,
            "evidence_type": self.evidence_type,
            "evidence_source": self.evidence_source,
            "evidence_snippet": self.evidence_snippet,
            "evidence_line": self.evidence_line,
            "evidence_confidence": float(self.evidence_confidence),
        }


@dataclass(slots=True)
class ReviewSummaryBlock:
    attempted: str = ""
    what_is_wrong: str = ""
    what_must_be_done_next: str = ""

    def to_dict(self) -> dict:
        return {
            "attempted": self.attempted,
            "what_is_wrong": self.what_is_wrong,
            "what_must_be_done_next": self.what_must_be_done_next,
        }


@dataclass(slots=True)
class AnalyzeTaskWorkflowResult:
    task_quality_summary: str = ""
    missing_details: list[str] = field(default_factory=list)
    concrete_questions: list[str] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    suggested_additions: list[str] = field(default_factory=list)
    repo_match: dict | None = None
    recommendation: str = ""
    technical_details: dict = field(default_factory=dict)
    technical_run: WorkflowRunLink | None = None

    def to_dict(self) -> dict:
        return {
            "task_quality_summary": self.task_quality_summary,
            "missing_details": list(self.missing_details),
            "concrete_questions": list(self.concrete_questions),
            "risks": list(self.risks),
            "suggested_additions": list(self.suggested_additions),
            "repo_match": dict(self.repo_match or {}) if self.repo_match is not None else None,
            "recommendation": self.recommendation,
            "technical_details": dict(self.technical_details or {}),
            "technical_run": self.technical_run.to_dict() if self.technical_run is not None else None,
        }


@dataclass(slots=True)
class StructureTaskWorkflowResult:
    title: str = ""
    summary: str = ""
    description: str = ""
    acceptance_criteria: list[str] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    open_questions: list[str] = field(default_factory=list)
    recommendation: str = ""
    technical_details: dict = field(default_factory=dict)
    technical_run: WorkflowRunLink | None = None

    def to_dict(self) -> dict:
        return {
            "title": self.title,
            "summary": self.summary,
            "description": self.description,
            "acceptance_criteria": list(self.acceptance_criteria),
            "risks": list(self.risks),
            "open_questions": list(self.open_questions),
            "recommendation": self.recommendation,
            "technical_details": dict(self.technical_details or {}),
            "technical_run": self.technical_run.to_dict() if self.technical_run is not None else None,
        }


@dataclass(slots=True)
class ImplementationPlanWorkflowResult:
    repo_match: str = ""
    repo_match_reason: str = ""
    configured_provider: str = ""
    repo_metadata_provider: str = ""
    allowlist_match: bool = False
    gitnexus_enabled: bool = False
    gitnexus_index_status: str = ""
    selection_decision: str = ""
    provider_used: str = ""
    provider_fallback: bool = False
    provider_reason: str = ""
    candidate_files_count: int = 0
    selected_files_count: int = 0
    top_candidate_files: list[SelectionCandidate] = field(default_factory=list)
    top_candidate_symbols: list[SelectionCandidate] = field(default_factory=list)
    top_closest_areas: list[AreaSuggestion] = field(default_factory=list)
    likely_files: list[str] = field(default_factory=list)
    likely_file_details: list[SelectionCandidate] = field(default_factory=list)
    likely_modules: list[str] = field(default_factory=list)
    likely_module_details: list[SelectionCandidate] = field(default_factory=list)
    closest_areas: list[AreaSuggestion] = field(default_factory=list)
    change_actions: list[FileChangeAction] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    validation_plan: list[str] = field(default_factory=list)
    execution_mode: str = ""
    selected_repos: list[dict] = field(default_factory=list)
    selected_files_by_repo: dict = field(default_factory=dict)
    writable_repo_id: str = ""
    writable_files: list[str] = field(default_factory=list)
    readonly_repo_ids: list[str] = field(default_factory=list)
    readonly_files_by_repo: dict = field(default_factory=dict)
    implementation_scope_summary: str = ""
    scope_enforcement_reason: str = ""
    recommendation: str = ""
    technical_details: dict = field(default_factory=dict)
    technical_run: WorkflowRunLink | None = None

    def to_dict(self) -> dict:
        return {
            "repo_match": self.repo_match,
            "repo_match_reason": self.repo_match_reason,
            "configured_provider": self.configured_provider,
            "repo_metadata_provider": self.repo_metadata_provider,
            "allowlist_match": bool(self.allowlist_match),
            "gitnexus_enabled": bool(self.gitnexus_enabled),
            "gitnexus_index_status": self.gitnexus_index_status,
            "selection_decision": self.selection_decision,
            "provider_used": self.provider_used,
            "provider_fallback": bool(self.provider_fallback),
            "provider_reason": self.provider_reason,
            "candidate_files_count": int(self.candidate_files_count),
            "selected_files_count": int(self.selected_files_count),
            "top_candidate_files": [item.to_dict() for item in self.top_candidate_files],
            "top_candidate_symbols": [item.to_dict() for item in self.top_candidate_symbols],
            "top_closest_areas": [item.to_dict() for item in self.top_closest_areas],
            "likely_files": list(self.likely_files),
            "likely_file_details": [item.to_dict() for item in self.likely_file_details],
            "likely_modules": list(self.likely_modules),
            "likely_module_details": [item.to_dict() for item in self.likely_module_details],
            "closest_areas": [item.to_dict() for item in self.closest_areas],
            "change_actions": [item.to_dict() for item in self.change_actions],
            "risks": list(self.risks),
            "validation_plan": list(self.validation_plan),
            "execution_mode": self.execution_mode,
            "selected_repos": list(self.selected_repos),
            "selected_files_by_repo": dict(self.selected_files_by_repo or {}),
            "writable_repo_id": self.writable_repo_id,
            "writable_files": list(self.writable_files),
            "readonly_repo_ids": list(self.readonly_repo_ids),
            "readonly_files_by_repo": dict(self.readonly_files_by_repo or {}),
            "implementation_scope_summary": self.implementation_scope_summary,
            "scope_enforcement_reason": self.scope_enforcement_reason,
            "recommendation": self.recommendation,
            "technical_details": dict(self.technical_details or {}),
            "technical_run": self.technical_run.to_dict() if self.technical_run is not None else None,
        }


@dataclass(slots=True)
class PreReviewWorkflowResult:
    verdict: str = ""
    review_verdict: str = ""
    decision_statement: str = ""
    review_summary: ReviewSummaryBlock | None = None
    review_verdict_confidence: float = 0.0
    final_change_summary: str = ""
    files_to_check: list[str] = field(default_factory=list)
    blocking_issues: list[str] = field(default_factory=list)
    global_blockers: list[str] = field(default_factory=list)
    issue_details: list[ReviewIssue] = field(default_factory=list)
    blocking_explanation: str = ""
    required_fixes: list[RequiredFix] = field(default_factory=list)
    ready_for_crucible: bool = False
    fix_and_retry_actionable: bool = False
    fix_and_retry_block_reason: str = ""
    execution_mode: str = ""
    selected_repos: list[dict] = field(default_factory=list)
    selected_files_by_repo: dict = field(default_factory=dict)
    writable_repo_id: str = ""
    writable_files: list[str] = field(default_factory=list)
    readonly_repo_ids: list[str] = field(default_factory=list)
    readonly_files_by_repo: dict = field(default_factory=dict)
    implementation_scope_summary: str = ""
    scope_enforcement_reason: str = ""
    recommendation: str = ""
    technical_details: dict = field(default_factory=dict)
    technical_run: WorkflowRunLink | None = None

    def to_dict(self) -> dict:
        return {
            "verdict": self.verdict,
            "review_verdict": self.review_verdict,
            "decision_statement": self.decision_statement,
            "review_summary": self.review_summary.to_dict() if self.review_summary is not None else None,
            "review_verdict_confidence": float(self.review_verdict_confidence),
            "final_change_summary": self.final_change_summary,
            "files_to_check": list(self.files_to_check),
            "blocking_issues": list(self.blocking_issues),
            "global_blockers": list(self.global_blockers),
            "issue_details": [item.to_dict() for item in self.issue_details],
            "blocking_explanation": self.blocking_explanation,
            "required_fixes": [item.to_dict() for item in self.required_fixes],
            "ready_for_crucible": bool(self.ready_for_crucible),
            "fix_and_retry_actionable": bool(self.fix_and_retry_actionable),
            "fix_and_retry_block_reason": self.fix_and_retry_block_reason,
            "execution_mode": self.execution_mode,
            "selected_repos": list(self.selected_repos),
            "selected_files_by_repo": dict(self.selected_files_by_repo or {}),
            "writable_repo_id": self.writable_repo_id,
            "writable_files": list(self.writable_files),
            "readonly_repo_ids": list(self.readonly_repo_ids),
            "readonly_files_by_repo": dict(self.readonly_files_by_repo or {}),
            "implementation_scope_summary": self.implementation_scope_summary,
            "scope_enforcement_reason": self.scope_enforcement_reason,
            "recommendation": self.recommendation,
            "technical_details": dict(self.technical_details or {}),
            "technical_run": self.technical_run.to_dict() if self.technical_run is not None else None,
        }
