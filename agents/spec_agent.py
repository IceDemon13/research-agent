from contracts.agent_result import AgentResult
from contracts.spec_parser import parse_spec_text
from logger_utils import log_line
from loops.react_loop import run_react_loop
from prompts.spec_prompt import SPEC_PROMPT
from tools.repo_tools import ensure_repo_context, format_repo_context


INSUFFICIENT_REVIEW_CONTEXT_MESSAGE = "Not enough repository context to review implementation"


def _build_spec_prompt_input(user_input: str, task_intent: str, repo_context: dict) -> str:
    files_used = repo_context.get("files_used") or []
    files_block = "\n".join(f"- {path}" for path in files_used) or "- none"
    resolved_target_files = repo_context.get("resolved_target_files") or []
    resolved_symbols = repo_context.get("resolved_symbols") or {}
    target_files_block = "\n".join(f"- {path}" for path in resolved_target_files) or "- none"
    symbols_block = "\n".join(
        f"- {symbol}: {', '.join(paths) if paths else 'no resolved file'}"
        for symbol, paths in resolved_symbols.items()
    ) or "- none"

    intent_guidance = ""
    if task_intent == "review":
        intent_guidance = (
            "Review mode requirements:\n"
            "- Describe existing implementation from repository context.\n"
            "- Explicitly name relevant files from the Files list.\n"
            "- Do not propose new files.\n"
            "- Do not frame the task as missing functionality by default.\n"
        )
    if resolved_target_files or resolved_symbols:
        intent_guidance += (
            "Target scope requirements:\n"
            "- Describe only the resolved target files or symbol implementation area.\n"
            "- Do not broaden scope to neighboring modules unless explicitly required by imports or registry wiring.\n"
            "- Do not add registry.py or other files unless the repository context clearly requires them.\n"
        )

    return (
        f"{format_repo_context(repo_context)}\n\n"
        f"Task intent: {task_intent}\n\n"
        f"Context files:\n{files_block}\n\n"
        f"Resolved target files:\n{target_files_block}\n\n"
        f"Resolved symbols:\n{symbols_block}\n\n"
        f"{intent_guidance}"
        f"Task:\n{user_input}"
    )


def run_spec_agent(
    user_input: str,
    task_intent: str = "create",
    repo_context: dict | None = None,
) -> AgentResult:
    resolved_repo_context = ensure_repo_context(user_input, ".", repo_context)
    if task_intent == "review" and not resolved_repo_context.get("chunks"):
        log_line("SPEC AGENT: Not enough repository context to review implementation")
        return AgentResult(
            agent_name="spec",
            output_text=INSUFFICIENT_REVIEW_CONTEXT_MESSAGE,
            success=False,
            task_intent=task_intent,
            repo_context=resolved_repo_context,
            metadata={
                "artifact_type": "spec",
                "spec": parse_spec_text(""),
            },
        )

    memory = [
        {
            "role": "system",
            "content": SPEC_PROMPT,
        }
    ]

    composed_input = _build_spec_prompt_input(
        user_input=user_input,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
    )

    answer, _messages = run_react_loop(
        user_input=composed_input,
        memory=memory,
        agent_name="spec_agent",
    )

    spec = parse_spec_text(answer)

    return AgentResult(
        agent_name="spec",
        output_text=answer,
        success=True,
        task_intent=task_intent,
        repo_context=resolved_repo_context,
        metadata={
            "artifact_type": "spec",
            "spec": spec,
        },
    )
