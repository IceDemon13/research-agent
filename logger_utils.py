from __future__ import annotations

from datetime import datetime
from pathlib import Path
import sys

from config import settings


CONSOLE_LEVELS = {
    "off": 100,
    "user": 50,
    "steps": 40,
    "info": 30,
    "debug": 20,
    "trace": 10,
}

FILE_LEVELS = {
    "off": 100,
    "error": 50,
    "warn": 40,
    "info": 30,
    "debug": 20,
    "trace": 10,
}

MOJIBAKE_MARKERS = (
    "Рђ",
    "Р‘",
    "Р’",
    "Р“",
    "Р”",
    "Р•",
    "РЄ",
    "РІ",
    "Рї",
    "СЃ",
    "С‚",
    "СЏ",
    "Рњ",
    "Рё",
)


def _normalize_console_level(value: str) -> str:
    normalized = (value or "").strip().lower()
    return normalized if normalized in CONSOLE_LEVELS else "steps"


def _normalize_file_level(value: str) -> str:
    normalized = (value or "").strip().lower()
    return normalized if normalized in FILE_LEVELS else "debug"


def _classify_console_level(message: str) -> str:
    text = (message or "").strip()

    if not text:
        return "trace"

    upper = text.upper()

    if any(token in upper for token in ("ERROR", "EXCEPTION", "FAILED", "TRACEBACK")):
        return "steps"

    if any(token in upper for token in ("NEW USER REQUEST", "STEP ", "FINAL ANSWER READY", "MAX STEPS REACHED")):
        return "steps"

    if any(
        token in upper
        for token in (
            "REACT_LOOP_VERSION",
            "AGENT NAME:",
            "MODEL:",
            "MESSAGES IN MEMORY:",
            "TOOLS AVAILABLE:",
            "TOOL SELECTED:",
            "TOOL BLOCKED:",
            "STEP ",
            "GATEWAY_AUDIT:",
        )
    ):
        return "info"

    if any(
        token in upper
        for token in (
            "TOOL ARGS:",
            "TOOL RESULT:",
            "ASSISTANT MESSAGE:",
            "FINAL ANSWER:",
            "USER:",
            "ARGS PARSE ERROR:",
            "UNKNOWN TOOL:",
            "TOOL EXECUTION ERROR:",
            "GATEWAY / LLM API ERROR:",
        )
    ):
        return "debug"

    return "trace"


def _classify_file_level(message: str) -> str:
    text = (message or "").strip()

    if not text:
        return "trace"

    upper = text.upper()

    if any(token in upper for token in ("ERROR", "EXCEPTION", "FAILED", "TRACEBACK")):
        return "error"

    if any(token in upper for token in ("BLOCKED", "MAX STEPS REACHED", "PARSE ERROR", "UNKNOWN TOOL")):
        return "warn"

    if any(
        token in upper
        for token in (
            "NEW USER REQUEST",
            "STEP ",
            "FINAL ANSWER READY",
            "REACT_LOOP_VERSION",
            "AGENT NAME:",
            "MODEL:",
            "MESSAGES IN MEMORY:",
            "TOOLS AVAILABLE:",
            "TOOL SELECTED:",
            "GATEWAY_AUDIT:",
        )
    ):
        return "info"

    if any(
        token in upper
        for token in (
            "TOOL ARGS:",
            "TOOL RESULT:",
            "ASSISTANT MESSAGE:",
            "FINAL ANSWER:",
            "USER:",
        )
    ):
        return "debug"

    return "trace"


def _should_log_console(message: str) -> bool:
    if not settings.log_to_console:
        return False

    current_level = _normalize_console_level(settings.log_level_console)
    message_level = _classify_console_level(message)

    return CONSOLE_LEVELS[message_level] >= CONSOLE_LEVELS[current_level]


def _should_log_file(message: str) -> bool:
    if not settings.log_to_file:
        return False

    current_level = _normalize_file_level(settings.log_level_file)
    message_level = _classify_file_level(message)

    return FILE_LEVELS[message_level] >= FILE_LEVELS[current_level]


def _trim_message(message: str, max_chars: int) -> str:
    if max_chars <= 0:
        return message

    if len(message) <= max_chars:
        return message

    return message[:max_chars] + "...[TRUNCATED]"


def ensure_utf8_console() -> None:
    for stream_name in ("stdout", "stderr"):
        stream = getattr(sys, stream_name, None)
        if stream is None or not hasattr(stream, "reconfigure"):
            continue
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            continue


def _looks_like_utf8_cp1251_mojibake(text: str) -> bool:
    if not text:
        return False
    return sum(text.count(marker) for marker in MOJIBAKE_MARKERS) >= 2


def _score_readable_cyrillic(text: str) -> int:
    score = 0
    for char in text:
        if "А" <= char <= "я" or char in "ЄєІіЇїҐґ":
            score += 2
    score -= sum(text.count(marker) for marker in MOJIBAKE_MARKERS) * 3
    return score


def _repair_utf8_cp1251_mojibake(text: str) -> str:
    try:
        return text.encode("cp1251", errors="strict").decode("utf-8", errors="strict")
    except Exception:
        return text


def _make_utf8_cp1251_mojibake(text: str) -> str:
    try:
        return text.encode("utf-8", errors="strict").decode("cp1251", errors="strict")
    except Exception:
        return text


def normalize_display_text(text: str) -> str:
    safe_text = str(text or "")
    if not _looks_like_utf8_cp1251_mojibake(safe_text):
        return safe_text

    repaired = _repair_utf8_cp1251_mojibake(safe_text)
    if _score_readable_cyrillic(repaired) > _score_readable_cyrillic(safe_text):
        return repaired
    return safe_text


def build_encoding_self_check() -> dict[str, object]:
    samples = ["Мета", "Ризики", "Що перевірити"]
    mojibake_samples = [_make_utf8_cp1251_mojibake(item) for item in samples]
    repaired = [normalize_display_text(item) for item in mojibake_samples]
    return {
        "ok": repaired == samples,
        "samples": samples,
        "mojibake_samples": mojibake_samples,
        "repaired": repaired,
    }


def _build_log_line(message: str) -> str:
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    return f"[{timestamp}] {message}"


def _get_log_file_path() -> Path:
    log_dir = Path(settings.log_dir)
    log_dir.mkdir(parents=True, exist_ok=True)
    return log_dir / settings.log_file_name


def log_line(message: str) -> None:
    safe_message = str(message)

    if _should_log_console(safe_message):
        ensure_utf8_console()
        console_message = normalize_display_text(safe_message)
        console_message = _trim_message(console_message, settings.log_console_max_chars)
        print(_build_log_line(console_message))

    if _should_log_file(safe_message):
        file_message = _trim_message(safe_message, settings.log_file_max_chars)
        log_file = _get_log_file_path()
        with log_file.open("a", encoding="utf-8") as f:
            f.write(_build_log_line(file_message) + "\n")
