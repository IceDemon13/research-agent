from __future__ import annotations

from console_app import main as run_console_app
from output_formatters import (
    format_pipeline_output as _format_pipeline_output,
    sanitize_pipeline_output as _sanitize_pipeline_output,
    split_answer_and_sources as _split_answer_and_sources,
)
from startup_text import build_terminal_startup_text


def main() -> None:
    run_console_app()


if __name__ == "__main__":
    main()
