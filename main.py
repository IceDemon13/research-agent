from agents.root_agent import (
    run_full_change_pipeline,
    run_full_draft_pipeline,
    run_lightweight_review_pipeline,
    run_root_agent,
    run_spec_to_code_pipeline,
)
from config import settings
from logger_utils import log_line


def _normalize_pipeline_console_mode(value: str) -> str:
    normalized = (value or "").strip().lower()
    if normalized in {"full", "summary", "off"}:
        return normalized
    return "summary"


def _summarize_pipeline_output(text: str, max_chars: int) -> str:
    safe_text = (text or "").strip()
    if max_chars <= 0 or len(safe_text) <= max_chars:
        return safe_text
    return safe_text[:max_chars].rstrip() + "\n...[TRUNCATED]"


def _format_pipeline_output(text: str) -> str:
    mode = _normalize_pipeline_console_mode(settings.pipeline_console_output_mode)
    safe_text = (text or "").strip()

    if mode == "off":
        return "(pipeline console output disabled)"
    if mode == "full":
        return safe_text
    return _summarize_pipeline_output(safe_text, settings.pipeline_console_preview_chars)


def _print_pipeline_section(title: str, text: str) -> None:
    print(f"\n[{title}]")
    formatted_output = _format_pipeline_output(text)
    if formatted_output:
        print(formatted_output)


def main() -> None:
    print("Research Agent started. Type 'exit' to quit.")
    print("For spec-to-code pipeline use: /pipeline <your request>")
    print("For full change pipeline use: /changes <your request>")
    print("For full draft pipeline use: /drafts <your request>")
    print("For full review pipeline use: /review <your request>")

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Bye!")
            break

        print("\n--- AGENT START ---")

        if user_input.startswith("/review "):
            pipeline_input = user_input[len("/review "):].strip()
            log_line("PIPELINE MODE: review_only")
            review_result = run_lightweight_review_pipeline(pipeline_input)

            _print_pipeline_section("Review Analysis", review_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/drafts "):
            pipeline_input = user_input[len("/drafts "):].strip()

            spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)
            _print_pipeline_section("Change Set Result", change_result.output_text)
            _print_pipeline_section("Draft Set Result", draft_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/changes "):
            pipeline_input = user_input[len("/changes "):].strip()

            spec_result, code_result, change_result = run_full_change_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)
            _print_pipeline_section("Change Set Result", change_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/pipeline "):
            pipeline_input = user_input[len("/pipeline "):].strip()

            spec_result, code_result = run_spec_to_code_pipeline(pipeline_input)

            _print_pipeline_section("Spec Result", spec_result.output_text)
            _print_pipeline_section("Code Plan Result", code_result.output_text)

            print("\n--- AGENT END ---")
            continue

        result = run_root_agent(user_input)
        print(f"\nAgent: {_format_pipeline_output(result.output_text)}")
        print("\n--- AGENT END ---")


if __name__ == "__main__":
    main()
