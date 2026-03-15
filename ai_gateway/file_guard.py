from __future__ import annotations

from pathlib import Path


class FileGuardError(Exception):
    pass


ALLOWED_OUTPUT_DIR = Path("output").resolve()
ALLOWED_EXTENSIONS = {".md", ".txt"}


def validate_output_filename(filename: str) -> Path:
    if not filename or not filename.strip():
        raise FileGuardError("Filename is empty.")

    raw_name = filename.strip()

    if "/" in raw_name or "\\" in raw_name:
        raise FileGuardError("Nested paths are not allowed.")

    if ".." in raw_name:
        raise FileGuardError("Parent path traversal is not allowed.")

    candidate = Path(raw_name)

    if candidate.is_absolute():
        raise FileGuardError("Absolute paths are not allowed.")

    if candidate.name != raw_name:
        raise FileGuardError("Only plain filenames are allowed.")

    suffix = candidate.suffix.lower()
    if suffix not in ALLOWED_EXTENSIONS:
        raise FileGuardError(
            f"File extension '{candidate.suffix}' is not allowed. "
            f"Allowed: {sorted(ALLOWED_EXTENSIONS)}"
        )

    safe_path = (ALLOWED_OUTPUT_DIR / candidate.name).resolve()

    if safe_path.parent != ALLOWED_OUTPUT_DIR:
        raise FileGuardError("Writing outside output directory is not allowed.")

    return safe_path


def to_display_output_path(path: Path) -> str:
    try:
        return path.relative_to(Path.cwd().resolve()).as_posix()
    except Exception:
        return path.as_posix()