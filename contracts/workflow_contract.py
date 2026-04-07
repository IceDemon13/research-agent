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
class ImplementationPlanPreviewItem:
    file: str = ""
    action: str = ""
    reason: str = ""
    likely_changes: str = ""
    risk: str = ""

    def to_dict(self) -> dict:
        return {
            "file": self.file,
            "action": self.action,
            "reason": self.reason,
            "likely_changes": self.likely_changes,
            "risk": self.risk,
        }


@dataclass(slots=True)
class ImplementationPlanBranchOption:
    option: str = ""
    plan: list[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "option": self.option,
            "plan": list(self.plan or []),
        }


@dataclass(slots=True)
class ImplementationPlanBranch:
    decision: str = ""
    options: list[ImplementationPlanBranchOption] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "decision": self.decision,
            "options": [item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(self.options or [])],
        }


@dataclass(slots=True)
class DraftPatchFileRationale:
    file: str = ""
    why: str = ""
    expected_effect: str = ""

    def to_dict(self) -> dict:
        return {
            "file": self.file,
            "why": self.why,
            "expected_effect": self.expected_effect,
        }


@dataclass(slots=True)
class DraftPatchWorkflowResult:
    repo_id: str = ""
    patch_generation_ready: bool = False
    patch_generation_blockers: list[str] = field(default_factory=list)
    allowed_files: list[str] = field(default_factory=list)
    generated_diff: str = ""
    diff_hash: str = ""
    file_rationales: list[DraftPatchFileRationale] = field(default_factory=list)
    patch_summary: str = ""
    validation_plan: list[str] = field(default_factory=list)
    validated: bool = False
    validation_status: str = ""
    validation_summary: str = ""
    repair_attempts: list[dict] = field(default_factory=list)
    repaired: bool = False
    execution_id: str = ""
    review_required: bool = True
    review_state: str = "pending"
    review_id: str = ""
    reviewed_by: str = ""
    confidence_score: int = 0
    novelty_score: int = 0
    apply_ready: bool = False
    apply_blockers: list[str] = field(default_factory=list)
    apply_mode: str = ""
    apply_modes_supported: list[str] = field(default_factory=list)
    applied: bool = False
    apply_artifact_path: str = ""
    commit_hash: str = ""
    auto_apply: bool = False
    technical_details: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "repo_id": self.repo_id,
            "patch_generation_ready": bool(self.patch_generation_ready),
            "patch_generation_blockers": list(self.patch_generation_blockers or []),
            "allowed_files": list(self.allowed_files or []),
            "generated_diff": self.generated_diff,
            "diff_hash": self.diff_hash,
            "file_rationales": [item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(self.file_rationales or [])],
            "patch_summary": self.patch_summary,
            "validation_plan": list(self.validation_plan or []),
            "validated": bool(self.validated),
            "validation_status": self.validation_status,
            "validation_summary": self.validation_summary,
            "repair_attempts": list(self.repair_attempts or []),
            "repaired": bool(self.repaired),
            "execution_id": self.execution_id,
            "review_required": bool(self.review_required),
            "review_state": self.review_state,
            "review_id": self.review_id,
            "reviewed_by": self.reviewed_by,
            "confidence_score": int(self.confidence_score),
            "novelty_score": int(self.novelty_score),
            "apply_ready": bool(self.apply_ready),
            "apply_blockers": list(self.apply_blockers or []),
            "apply_mode": self.apply_mode,
            "apply_modes_supported": list(self.apply_modes_supported or []),
            "applied": bool(self.applied),
            "apply_artifact_path": self.apply_artifact_path,
            "commit_hash": self.commit_hash,
            "auto_apply": bool(self.auto_apply),
            "technical_details": dict(self.technical_details or {}),
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
    quality_score: int = 0
    confidence_score: int = 0
    novelty_score: int = 0
    domain_novelty_score: int = 0
    repo_novelty_score: int = 0
    repo_confidence: int = 0
    file_confidence: int = 0
    task_confidence: int = 0
    quality_state: str = ""
    quality_breakdown: dict = field(default_factory=dict)
    novelty_level: str = ""
    analysis_mode: str = ""
    advisory_block_title: str = ""
    missing_details: list[str] = field(default_factory=list)
    concrete_questions: list[str] = field(default_factory=list)
    decision_questions: list[str] = field(default_factory=list)
    risks: list[str] = field(default_factory=list)
    suggested_additions: list[str] = field(default_factory=list)
    repo_match: dict | None = None
    selected_repos: list[dict] = field(default_factory=list)
    top_historical_matches: list[dict] = field(default_factory=list)
    top_historical_changed_files: list[str] = field(default_factory=list)
    candidate_files_count: int = 0
    selected_files_count: int = 0
    top_candidate_files: list[SelectionCandidate] = field(default_factory=list)
    implementation_plan_preview: list[ImplementationPlanPreviewItem] = field(default_factory=list)
    implementation_plan_branches: list[ImplementationPlanBranch] = field(default_factory=list)
    patch_generation_ready: bool = False
    patch_generation_blockers: list[str] = field(default_factory=list)
    patch_generation_allowed_files: list[str] = field(default_factory=list)
    recommendation: str = ""
    technical_details: dict = field(default_factory=dict)
    technical_run: WorkflowRunLink | None = None

    def to_dict(self) -> dict:
        return {
            "task_quality_summary": self.task_quality_summary,
            "quality_score": int(self.quality_score),
            "confidence_score": int(self.confidence_score),
            "novelty_score": int(self.novelty_score),
            "domain_novelty_score": int(self.domain_novelty_score),
            "repo_novelty_score": int(self.repo_novelty_score),
            "repo_confidence": int(self.repo_confidence),
            "file_confidence": int(self.file_confidence),
            "task_confidence": int(self.task_confidence),
            "quality_state": self.quality_state,
            "quality_breakdown": dict(self.quality_breakdown or {}),
            "novelty_level": self.novelty_level,
            "analysis_mode": self.analysis_mode,
            "advisory_block_title": self.advisory_block_title,
            "missing_details": list(self.missing_details),
            "concrete_questions": list(self.concrete_questions),
            "decision_questions": list(self.decision_questions),
            "risks": list(self.risks),
            "suggested_additions": list(self.suggested_additions),
            "repo_match": dict(self.repo_match or {}) if self.repo_match is not None else None,
            "selected_repos": [dict(item or {}) for item in list(self.selected_repos or [])],
            "top_historical_matches": [dict(item or {}) for item in list(self.top_historical_matches or [])],
            "top_historical_changed_files": list(self.top_historical_changed_files),
            "candidate_files_count": int(self.candidate_files_count),
            "selected_files_count": int(self.selected_files_count),
            "top_candidate_files": [item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(self.top_candidate_files or [])],
            "implementation_plan_preview": [item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(self.implementation_plan_preview or [])],
            "implementation_plan_branches": [item.to_dict() if hasattr(item, "to_dict") else dict(item or {}) for item in list(self.implementation_plan_branches or [])],
            "patch_generation_ready": bool(self.patch_generation_ready),
            "patch_generation_blockers": list(self.patch_generation_blockers or []),
            "patch_generation_allowed_files": list(self.patch_generation_allowed_files or []),
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
