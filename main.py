from agents.root_agent import (
    run_full_change_pipeline,
    run_full_draft_pipeline,
    run_root_agent,
    run_spec_to_code_pipeline,
)
from config import settings


def _preview(text: str, max_chars: int) -> str:
    value = (text or "").strip()
    if max_chars <= 0 or len(value) <= max_chars:
        return value
    return value[:max_chars] + "\n...[TRUNCATED]"


def _print_pipeline_block(title: str, content: str) -> None:
    mode = (settings.pipeline_console_output_mode or "summary").strip().lower()

    if mode == "off":
        return

    print(f"\n[{title}]")

    if mode == "full":
        print(content)
        return

    preview_chars = settings.pipeline_console_preview_chars
    print(_preview(content, preview_chars))


def main() -> None:
    print("Research Agent started. Type 'exit' to quit.")
    print("For spec-to-code pipeline use: /pipeline <your request>")
    print("For full change pipeline use: /changes <your request>")
    print("For full draft pipeline use: /drafts <your request>")

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Bye!")
            break

        print("\n--- AGENT START ---")

        if user_input.startswith("/drafts "):
            pipeline_input = user_input[len("/drafts "):].strip()

            spec_result, code_result, change_result, draft_result = run_full_draft_pipeline(pipeline_input)

            _print_pipeline_block("Spec Result", spec_result.output_text)
            _print_pipeline_block("Code Plan Result", code_result.output_text)
            _print_pipeline_block("Change Set Result", change_result.output_text)
            _print_pipeline_block("Draft Set Result", draft_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/changes "):
            pipeline_input = user_input[len("/changes "):].strip()

            spec_result, code_result, change_result = run_full_change_pipeline(pipeline_input)

            _print_pipeline_block("Spec Result", spec_result.output_text)
            _print_pipeline_block("Code Plan Result", code_result.output_text)
            _print_pipeline_block("Change Set Result", change_result.output_text)

            print("\n--- AGENT END ---")
            continue

        if user_input.startswith("/pipeline "):
            pipeline_input = user_input[len("/pipeline "):].strip()

            spec_result, code_result = run_spec_to_code_pipeline(pipeline_input)

            _print_pipeline_block("Spec Result", spec_result.output_text)
            _print_pipeline_block("Code Plan Result", code_result.output_text)

            print("\n--- AGENT END ---")
            continue

        result = run_root_agent(user_input)
        print(f"\nAgent: {result.output_text}")
        print("\n--- AGENT END ---")


if __name__ == "__main__":
    main()