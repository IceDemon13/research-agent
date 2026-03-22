from __future__ import annotations

import argparse
import io
import contextlib
import sys

from services.auth_service import AuthError, AuthService

from console_app import main as run_console_app
from output_formatters import (
    format_pipeline_output as _format_pipeline_output,
    sanitize_pipeline_output as _sanitize_pipeline_output,
    split_answer_and_sources as _split_answer_and_sources,
)
from startup_text import build_terminal_startup_text


def _handle_reset_password_command(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Reset a local user password by username")
    parser.add_argument("username", help="Username to reset")
    parser.add_argument("--password", default="", help="Manual replacement password")
    parser.add_argument("--generate", action="store_true", help="Generate a secure password automatically")
    parser.add_argument(
        "--no-must-change-password",
        action="store_true",
        help="Do not force password change on next login",
    )
    args = parser.parse_args(argv)

    service = AuthService()
    try:
        result = service.reset_password_by_username(
            args.username,
            generate_password=bool(args.generate or not str(args.password or "").strip()),
            password=str(args.password or ""),
            must_change_password=not bool(args.no_must_change_password),
        )
    except AuthError as exc:
        print(f"Password reset failed: {exc}")
        return 1

    print(f"Password reset for '{args.username}'.")
    print(f"Must change password: {'yes' if result.user.must_change_password else 'no'}")
    if result.generated_password:
        print(f"Generated password: {result.generated_password}")
    return 0


def main() -> None:
    if len(sys.argv) > 1 and str(sys.argv[1] or "").strip().lower() == "reset-password":
        raise SystemExit(_handle_reset_password_command(sys.argv[2:]))
    run_console_app()


if __name__ == "__main__":
    main()
