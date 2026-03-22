from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from config import settings


@dataclass(frozen=True, slots=True)
class ModelRoutingDecision:
    model_used: str = ""
    routing_reason: str = ""
    was_escalated: bool = False
    source_stage: str = "initial"
    estimated_prompt_size: int = 0

    def to_metadata(self) -> dict[str, Any]:
        return {
            "model_used": str(self.model_used or "").strip(),
            "routing_reason": str(self.routing_reason or "").strip(),
            "was_escalated": bool(self.was_escalated),
            "source_stage": str(self.source_stage or "").strip() or "initial",
            "estimated_prompt_size": int(self.estimated_prompt_size or 0),
            "model_routing": {
                "model_used": str(self.model_used or "").strip(),
                "routing_reason": str(self.routing_reason or "").strip(),
                "was_escalated": bool(self.was_escalated),
                "source_stage": str(self.source_stage or "").strip() or "initial",
                "estimated_prompt_size": int(self.estimated_prompt_size or 0),
            },
        }


def _safe_context(context: dict | None) -> dict[str, Any]:
    return dict(context or {}) if isinstance(context, dict) else {}


def _selected_files(context: dict | None) -> list[str]:
    repo_context = _safe_context(context)
    return [
        str(item).strip()
        for item in list(repo_context.get("files_used", []) or [])
        if str(item).strip()
    ]


def _closest_areas(context: dict | None) -> list[Any]:
    repo_context = _safe_context(context)
    areas = list(repo_context.get("closest_areas", []) or [])
    if areas:
        return areas
    summary = repo_context.get("repo_context_summary")
    if isinstance(summary, dict):
        return list(summary.get("closest_areas", []) or [])
    return []


def _closest_area_confidence(areas: list[Any]) -> float:
    highest = 0.0
    for area in list(areas or []):
        if not isinstance(area, dict):
            continue
        try:
            highest = max(highest, float(area.get("confidence", 0.0) or 0.0))
        except (TypeError, ValueError):
            continue
    return highest


def _repo_profile(context: dict | None) -> dict[str, Any]:
    repo_context = _safe_context(context)
    profile = repo_context.get("repo_profile")
    if isinstance(profile, dict):
        return dict(profile)
    summary = repo_context.get("repo_context_summary")
    if isinstance(summary, dict) and isinstance(summary.get("repo_profile"), dict):
        return dict(summary.get("repo_profile"))
    return {}


def _bool(value: Any) -> bool:
    return bool(value)


def _override_model(name: str, mapping: dict[str, str]) -> str:
    key = str(name or "").strip().lower()
    return str(mapping.get(key, "") or "").strip()


def _light_model() -> str:
    return str(settings.llm.default_light_model or settings.llm.model_name or "gpt-5.4-mini").strip()


def _heavy_model() -> str:
    return str(settings.llm.default_heavy_model or settings.llm.strong_model_name or _light_model()).strip()


def _estimate_prompt_size(prompt_text: str, repo_context: dict | None = None) -> int:
    size = len(str(prompt_text or "").strip())
    context = _safe_context(repo_context)
    size += sum(len(str(path).strip()) for path in _selected_files(context))
    size += min(4000, sum(len(str(chunk.get("snippet", "") or "")) for chunk in list(context.get("chunks", []) or []) if isinstance(chunk, dict)))
    return size


def _apply_budget_guard(
    *,
    decision_model: str,
    current_heavy_calls: int,
    max_heavy_calls: int,
    fallback_to_light: bool,
    reason: str,
    source_stage: str,
    estimated_prompt_size: int,
) -> ModelRoutingDecision:
    if decision_model != _heavy_model():
        return ModelRoutingDecision(
            model_used=decision_model,
            routing_reason=reason,
            was_escalated=decision_model == _heavy_model(),
            source_stage=source_stage,
            estimated_prompt_size=estimated_prompt_size,
        )
    if max_heavy_calls > 0 and current_heavy_calls >= max_heavy_calls and fallback_to_light:
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason=f"{reason}; downgraded to light model because heavy-call budget was exceeded",
            was_escalated=False,
            source_stage=source_stage,
            estimated_prompt_size=estimated_prompt_size,
        )
    return ModelRoutingDecision(
        model_used=decision_model,
        routing_reason=reason,
        was_escalated=decision_model == _heavy_model(),
        source_stage=source_stage,
        estimated_prompt_size=estimated_prompt_size,
    )


def route_model(
    *,
    workflow_name: str = "",
    technical_mode: str = "",
    stage_name: str = "",
    user_input: str = "",
    repo_context: dict | None = None,
    has_artifact: bool = False,
    actionable: bool = False,
    repo_mismatch: bool = False,
    current_heavy_calls: int = 0,
    max_heavy_calls: int | None = None,
) -> ModelRoutingDecision:
    workflow = str(workflow_name or "").strip().lower()
    technical = str(technical_mode or "").strip().lower()
    stage = str(stage_name or "").strip().lower() or technical or workflow or "initial"
    context = _safe_context(repo_context)
    profile = _repo_profile(context)
    selected_files = _selected_files(context)
    closest_areas = _closest_areas(context)
    candidate_count = int(context.get("candidate_files_count", 0) or 0)
    selected_count = int(context.get("selected_files_count", len(selected_files)) or len(selected_files))
    primary_stack = str(profile.get("primary_stack", "") or "").strip().lower()
    project_count = int(profile.get("project_count", 0) or 0)
    controller_count = int(profile.get("controller_count", 0) or 0)
    handler_count = int(profile.get("handler_count", 0) or 0)
    query_length = len(str(user_input or "").strip())
    ambiguous_selection = not selected_files and bool(closest_areas)
    closest_area_confidence = _closest_area_confidence(closest_areas)
    weak_closest_areas_only = not selected_files and bool(closest_areas) and closest_area_confidence < 0.45
    conflicting_evidence = selected_count > 0 and candidate_count >= max(6, selected_count * 4)
    complex_dotnet_repo = primary_stack == "dotnet" and (
        project_count >= 4 or controller_count >= 6 or handler_count >= 10
    )
    max_heavy = int(max_heavy_calls if max_heavy_calls is not None else settings.llm.max_heavy_calls_per_workflow or 0)
    fallback_to_light = bool(settings.llm.fallback_to_light_on_budget_exceeded)
    estimated_prompt_size = _estimate_prompt_size(user_input, context)

    if repo_mismatch:
        return ModelRoutingDecision(
            model_used="",
            routing_reason="Deterministic short-circuit: repo mismatch is strong, so no heavy reasoning is justified.",
            was_escalated=False,
            source_stage="deterministic",
            estimated_prompt_size=estimated_prompt_size,
        )

    workflow_override = _override_model(workflow, settings.llm.workflow_model_overrides)
    technical_override = _override_model(technical, settings.llm.technical_run_model_overrides)
    if workflow_override or technical_override:
        chosen = workflow_override or technical_override
        return ModelRoutingDecision(
            model_used=chosen,
            routing_reason=f"Configured override selected model '{chosen}'.",
            was_escalated=chosen == _heavy_model(),
            source_stage="initial",
            estimated_prompt_size=estimated_prompt_size,
        )

    if workflow == "analyze_task":
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason="Analyze-task workflow uses the light model by default.",
            estimated_prompt_size=estimated_prompt_size,
        )
    if workflow == "structure_task":
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason="Structure-task workflow is repo-blind and uses the light model.",
            estimated_prompt_size=estimated_prompt_size,
        )
    if workflow == "implementation_plan":
        if weak_closest_areas_only:
            return ModelRoutingDecision(
                model_used=_light_model(),
                routing_reason="Implementation-plan stayed on the light model because only weak closest-area evidence was available.",
                estimated_prompt_size=estimated_prompt_size,
            )
        if ambiguous_selection or conflicting_evidence or complex_dotnet_repo or query_length >= 500:
            return _apply_budget_guard(
                decision_model=_heavy_model(),
                current_heavy_calls=current_heavy_calls,
                max_heavy_calls=max_heavy,
                fallback_to_light=fallback_to_light,
                reason="Implementation-plan synthesis escalated because repo evidence is ambiguous or the repo is structurally complex.",
                source_stage="escalated",
                estimated_prompt_size=estimated_prompt_size,
            )
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason="Implementation-plan workflow stayed on the light model because selected context is concrete.",
            estimated_prompt_size=estimated_prompt_size,
        )
    if workflow == "pre_review":
        if not has_artifact:
            return ModelRoutingDecision(
                model_used="",
                routing_reason="Deterministic short-circuit: pre-review has no concrete implementation artifact or diff.",
                was_escalated=False,
                source_stage="deterministic",
                estimated_prompt_size=estimated_prompt_size,
            )
        return _apply_budget_guard(
            decision_model=_heavy_model(),
            current_heavy_calls=current_heavy_calls,
            max_heavy_calls=max_heavy,
            fallback_to_light=fallback_to_light,
            reason="Pre-review uses the heavy model only when concrete implementation evidence exists.",
            source_stage="initial",
            estimated_prompt_size=estimated_prompt_size,
        )
    if workflow == "fix_and_retry":
        if not actionable:
            return ModelRoutingDecision(
                model_used="",
                routing_reason="Deterministic short-circuit: fix-and-retry is not actionable.",
                was_escalated=False,
                source_stage="deterministic",
                estimated_prompt_size=estimated_prompt_size,
            )
        return _apply_budget_guard(
            decision_model=_heavy_model(),
            current_heavy_calls=current_heavy_calls,
            max_heavy_calls=max_heavy,
            fallback_to_light=fallback_to_light,
            reason="Fix-and-retry uses the heavy model only after actionable gating passes.",
            source_stage="initial",
            estimated_prompt_size=estimated_prompt_size,
        )

    if technical == "implement":
        return _apply_budget_guard(
            decision_model=_heavy_model(),
            current_heavy_calls=current_heavy_calls,
            max_heavy_calls=int(settings.llm.max_heavy_calls_per_run or max_heavy or 0),
            fallback_to_light=fallback_to_light,
            reason="Technical implementation stages use the heavy model.",
            source_stage="initial",
            estimated_prompt_size=estimated_prompt_size,
        )
    if technical == "review":
        if has_artifact and selected_files:
            return _apply_budget_guard(
                decision_model=_heavy_model(),
                current_heavy_calls=current_heavy_calls,
                max_heavy_calls=int(settings.llm.max_heavy_calls_per_run or max_heavy or 0),
                fallback_to_light=fallback_to_light,
                reason="Technical review escalated because repo-aware artifact evidence is available.",
                source_stage="escalated",
                estimated_prompt_size=estimated_prompt_size,
            )
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason="Technical review stayed on the light model because strong artifact evidence was not present.",
            estimated_prompt_size=estimated_prompt_size,
        )
    if technical == "spec":
        if weak_closest_areas_only:
            return ModelRoutingDecision(
                model_used=_light_model(),
                routing_reason="Technical spec stayed on the light model because only weak closest-area evidence was available.",
                estimated_prompt_size=estimated_prompt_size,
            )
        if complex_dotnet_repo and ambiguous_selection:
            return _apply_budget_guard(
                decision_model=_heavy_model(),
                current_heavy_calls=current_heavy_calls,
                max_heavy_calls=int(settings.llm.max_heavy_calls_per_run or max_heavy or 0),
                fallback_to_light=fallback_to_light,
                reason="Technical spec escalated because repo-aware context is complex and ambiguous.",
                source_stage="escalated",
                estimated_prompt_size=estimated_prompt_size,
            )
        return ModelRoutingDecision(
            model_used=_light_model(),
            routing_reason="Technical spec uses the light model by default.",
            estimated_prompt_size=estimated_prompt_size,
        )

    return ModelRoutingDecision(
        model_used=_light_model(),
        routing_reason="Defaulted to the light model.",
        estimated_prompt_size=estimated_prompt_size,
    )


def merge_model_metadata(metadata: dict[str, Any] | None, decision: ModelRoutingDecision) -> dict[str, Any]:
    merged = dict(metadata or {})
    merged.update(decision.to_metadata())
    return merged
