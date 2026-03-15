from ai_gateway.file_guard import (
    FileGuardError,
    to_display_output_path,
    validate_output_filename,
)


def write_report(filename: str, content: str) -> str:
    """Save a markdown report into the output folder."""
    try:
        safe_path = validate_output_filename(filename)
        safe_path.parent.mkdir(parents=True, exist_ok=True)
        safe_path.write_text(content, encoding="utf-8")

        display_path = to_display_output_path(safe_path)
        return f"Report saved to {display_path}"

    except FileGuardError as e:
        return f"write_report blocked: {e}"
    except Exception as e:
        return f"write_report error: {e}"