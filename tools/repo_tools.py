from __future__ import annotations

from pathlib import Path


IGNORED_DIRS = {
    ".git",
    ".idea",
    ".vscode",
    ".vs",
    ".venv",
    "venv",
    "__pycache__",
    "node_modules",
    "site-packages",
    "dist",
    "build",
    "logs",
    "output",
    "example_output",
}

ALLOWED_EXTENSIONS = {
    ".py",
    ".cs",
    ".json",
    ".yml",
    ".yaml",
    ".xml",
    ".sql",
    ".md",
    ".txt",
    ".js",
    ".ts",
    ".tsx",
    ".jsx",
    ".html",
    ".css",
}


def _is_ignored(path: Path) -> bool:
    parts = {part.lower() for part in path.parts}
    ignored = {part.lower() for part in IGNORED_DIRS}
    return bool(parts & ignored)


def list_repo_files(root: str = ".", max_files: int = 200) -> str:
    root_path = Path(root).resolve()

    if not root_path.exists():
        return f"Root path does not exist: {root}"

    if not root_path.is_dir():
        return f"Root path is not a directory: {root}"

    files: list[str] = []

    for path in root_path.rglob("*"):
        if len(files) >= max_files:
            break

        if not path.is_file():
            continue

        if _is_ignored(path):
            continue

        if path.suffix.lower() not in ALLOWED_EXTENSIONS:
            continue

        rel = path.relative_to(root_path).as_posix()
        files.append(rel)

    if not files:
        return "No matching repository files found."

    return "\n".join(files)


def read_repo_file(path: str, max_chars: int = 6000) -> str:
    root_path = Path(".").resolve()
    raw_path = (path or "").strip()

    if not raw_path:
        return "File path is empty."

    if raw_path.startswith("/") or raw_path.startswith("\\"):
        return "Absolute paths are not allowed."

    if ".." in Path(raw_path).parts:
        return "Parent path traversal is not allowed."

    candidate = (root_path / raw_path).resolve()

    if not candidate.exists():
        return f"File not found: {raw_path}"

    if not candidate.is_file():
        return f"Path is not a file: {raw_path}"

    if _is_ignored(candidate):
        return f"Access denied for file: {raw_path}"

    if candidate.suffix.lower() not in ALLOWED_EXTENSIONS:
        return f"File type is not allowed: {raw_path}"

    try:
        text = candidate.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        try:
            text = candidate.read_text(encoding="utf-8-sig")
        except Exception as e:
            return f"Failed to read file: {e}"
    except Exception as e:
        return f"Failed to read file: {e}"

    if len(text) > max_chars:
        text = text[:max_chars] + "\n...[TRUNCATED]"

    return f"# FILE: {raw_path}\n\n{text}"