import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from services.dry_run_write_evaluation_service import DryRunWriteEvaluationService
from services.routing_benchmark_service import _normalize_file_list, _safe_text

EXCLUDED = {'TEL-13491','TEL-13458','TEL-13375','TEL-13502','TEL-13394'}
HOLDOUT_PATH = '/app/artifacts/routing_benchmarks/generated_cases_canonical_single_repo_mainline_2026_04_04_eval_holdout_20260404T194005Z.json'
with open(HOLDOUT_PATH, encoding='utf-8') as f:
    payload = json.load(f)
all_cases = [dict(item) for item in payload.get('cases', []) if isinstance(item, dict)]
remaining = [c for c in all_cases if _safe_text(c.get('jira_key', '')).upper() not in EXCLUDED]
service = DryRunWriteEvaluationService(artifacts_root='/app/artifacts/planning_audit', per_case_timeout_seconds=20)
dataset = service.build_dataset(remaining, min_cases=2, max_cases=2)
cohort = list(dataset.get('cases', []) or [])
rows = []
for case in cohort:
    result = service.run_case(case, execution_mode='full_dry_run')
    writable_repo_id = _safe_text(result.get('writable_repo_id', ''))
    expected_by_repo = dict(case.get('expected_files_by_repo', {}) or {})
    expected_files = _normalize_file_list(expected_by_repo.get(writable_repo_id, case.get('expected_files', [])))
    selected_file = _safe_text(result.get('bounded_primary_target', '')) or _safe_text(result.get('canonical_selected_file_used_for_execution', ''))
    file_hit = bool(selected_file and selected_file in expected_files)
    chosen_class = _safe_text(result.get('canonical_selected_class_used_for_execution', '')) or 'Unknown'
    chosen_method = _safe_text(result.get('canonical_selected_method_used_for_execution', '')) or 'Unknown'
    class_hit = bool(file_hit and chosen_class not in ('', 'Unknown'))
    method_hit = bool(file_hit and chosen_method not in ('', 'Unknown'))
    validation = dict(result.get('validation_result', {}) or {})
    final_bucket = (
        _safe_text(result.get('validation_outcome_split', ''))
        or _safe_text(validation.get('validation_outcome_split', ''))
        or _safe_text(validation.get('outcome_type', ''))
        or _safe_text(result.get('bounded_downgraded_to_draft_reason', ''))
        or _safe_text(result.get('generation_status', ''))
        or 'unknown'
    )
    rows.append({
        'jira_key': _safe_text(result.get('jira_key', '')),
        'selected_file': selected_file,
        'chosen_class': chosen_class,
        'chosen_method': chosen_method,
        'patch_generated': bool(result.get('patch_generated', False)),
        'apply_succeeded': bool(result.get('apply_succeeded', False)),
        'restore_passed': bool(result.get('restore_passed', False)),
        'build_passed': bool(result.get('compile_passed', False)),
        'targeted_test_started': bool(result.get('targeted_test_started', False)),
        'targeted_test_passed': bool(result.get('targeted_test_passed', False)),
        'final_bucket': final_bucket,
        'file_localization_hit': file_hit,
        'class_localization_hit': class_hit,
        'method_localization_hit': method_hit,
        'generation_status': _safe_text(result.get('generation_status', '')),
        'validation_outcome_split': _safe_text(result.get('validation_outcome_split', '')),
    })
count = len(rows)
def rate(v): return round(v / count, 4) if count else 0.0
artifact = {
    'artifact_type': 'canonical_single_repo_mainline_holdout_eval',
    'generated_at': datetime.now(timezone.utc).isoformat(),
    'canonical_mainline_doc': 'docs/canonical_mainline_codegen_state_2026-04-04.md',
    'holdout_definition': {
        'source_eval_holdout_artifact': HOLDOUT_PATH,
        'selection': 'fresh leakage-free single-repo eval holdout excluding canonical debug tickets, then round-robin capped to 2 unseen cases for bounded runtime',
        'excluded_jira_keys': sorted(EXCLUDED),
        'remaining_candidate_count_after_exclusion': len(remaining),
        'cohort_count': len(cohort),
        'cohort_jira_keys': [_safe_text(c.get('jira_key', '')) for c in cohort],
        'dataset_composition': dict(dataset.get('composition', {}) or {}),
        'per_case_timeout_seconds': 20,
        'class_method_metric_note': 'class/method localization hit rates are operational rates against canonical grounded execution selections on the historically correct file; the holdout cases do not persist historical class/method truth.'
    },
    'aggregate_metrics': {
        'total_cases': count,
        'file_localization_hit_rate': rate(sum(1 for r in rows if r['file_localization_hit'])),
        'class_localization_hit_rate': rate(sum(1 for r in rows if r['class_localization_hit'])),
        'method_localization_hit_rate': rate(sum(1 for r in rows if r['method_localization_hit'])),
        'patch_generated_rate': rate(sum(1 for r in rows if r['patch_generated'])),
        'build_valid_rate': rate(sum(1 for r in rows if r['build_passed'] or r['final_bucket'] == 'build_valid_test_env_blocked')),
        'targeted_test_started_rate': rate(sum(1 for r in rows if r['targeted_test_started'])),
        'dominant_failure_buckets': dict(Counter(r['final_bucket'] for r in rows).most_common()),
    },
    'rows': rows,
}
out = Path('/app/artifacts/planning_audit/canonical_single_repo_mainline_holdout_eval_background_latest.json')
out.write_text(json.dumps(artifact, ensure_ascii=False, indent=2), encoding='utf-8')
print(out.as_posix())
