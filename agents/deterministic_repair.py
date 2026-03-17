from __future__ import annotations

import re

from contracts.draft_set import DraftSet
from contracts.file_draft import FileDraft


def _find_file(draft_set: DraftSet, path: str) -> FileDraft | None:
    for item in draft_set.files:
        if item.path == path:
            return item
    return None


def _extract_command_handlers(text: str) -> list[str]:
    pattern = re.compile(r'CommandHandler\(\s*["\']([^"\']+)["\']\s*,\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\)')
    return [handler_name for _command, handler_name in pattern.findall(text)]


def _has_function(text: str, func_name: str) -> bool:
    return re.search(rf"async\s+def\s+{re.escape(func_name)}\s*\(", text) is not None or re.search(
        rf"def\s+{re.escape(func_name)}\s*\(",
        text,
    ) is not None


def _build_missing_telegram_handler(func_name: str) -> str:
    return f"""

async def {func_name}(update: Update, context: ContextTypes.DEFAULT_TYPE):
    message = update.effective_message
    if not message:
        return

    await message.reply_text("Команда тимчасово в розробці.")
"""


def _ensure_telegram_imports(text: str) -> str:
    need_update = "Update" not in text
    need_context = "ContextTypes" not in text

    if not (need_update or need_context):
        return text

    old = "from telegram.ext import Application, CommandHandler, ContextTypes, MessageHandler, filters"
    if old in text:
        return text

    return text


def _fix_telegram_bot_handlers(draft_set: DraftSet) -> tuple[DraftSet, list[str]]:
    target = _find_file(draft_set, "telegram_bot.py")
    if target is None:
        return draft_set, []

    text = target.content
    handlers = _extract_command_handlers(text)
    added: list[str] = []

    for handler_name in handlers:
        if not _has_function(text, handler_name):
            text += _build_missing_telegram_handler(handler_name)
            added.append(f"telegram_bot.py: added missing handler `{handler_name}`")

    if added:
        target.content = text

    return draft_set, added


def run_deterministic_repair(draft_set: DraftSet) -> tuple[DraftSet, list[str]]:
    all_fixes: list[str] = []

    draft_set, fixes = _fix_telegram_bot_handlers(draft_set)
    all_fixes.extend(fixes)

    return draft_set, all_fixes