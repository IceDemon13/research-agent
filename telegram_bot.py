from __future__ import annotations

import os
import tempfile

from telegram import Update
from telegram.ext import Application, CommandHandler, ContextTypes, MessageHandler, filters

from agents.root_agent import (
    run_brief_export_from_task_brief,
    run_review_spec_from_task_brief,
    run_root_agent,
    run_spec_export_from_task_brief,
    run_task_intake,
)
from config import settings


def _chunk_text(text: str, chunk_size: int = 4000) -> list[str]:
    value = (text or "").strip()
    if not value:
        return [""]

    return [value[i:i + chunk_size] for i in range(0, len(value), chunk_size)]


def build_start_message_clean() -> str:
    return (
        "Привіт. Я можу допомогти з аналізом і підготовкою змін у репозиторії.\n\n"
        "Що я вмію:\n"
        "- робити review існуючого коду\n"
        "- готувати drafts для точкових змін у функціях і файлах\n"
        "- формувати change set для нових helper/module змін\n"
        "- готувати spec перед реалізацією\n\n"
        "Приклади запитів:\n"
        "- /review review existing search_in_repo implementation in repo_tools\n"
        "- /drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py\n"
        "- /drafts add input validation logging to read_file_range in tools/repo_tools.py\n"
        "- /changes create helper to export repo manifest summary as markdown\n"
        "- /spec підготуй специфікацію для додавання більш детального логування в search_in_repo\n\n"
        "Важливо:\n"
        "для складних функцій я можу повернути safe fallback suggestion замість ризикованого rewrite."
    )


def build_start_message() -> str:
    return (
        "Привіт. Я можу допомогти з аналізом і підготовкою змін у репозиторії.\n\n"
        "Що я вмію:\n"
        "- робити review існуючого коду\n"
        "- готувати drafts для точкових змін у функціях і файлах\n"
        "- формувати change set для нових helper/module змін\n"
        "- готувати spec перед реалізацією\n\n"
        "Приклади запитів:\n"
        "- /review review existing search_in_repo implementation in repo_tools\n"
        "- /drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py\n"
        "- /drafts add input validation logging to read_file_range in tools/repo_tools.py\n"
        "- /changes create helper to export repo manifest summary as markdown\n"
        "- /spec підготуй специфікацію для додавання більш детального логування в search_in_repo\n\n"
        "Важливо:\n"
        "для складних функцій я можу повернути safe fallback suggestion замість ризикованого rewrite."
    )


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.message.reply_text(build_start_message_clean())
    return
    await update.message.reply_text(
        "Привіт. Я можу допомогти з аналізом і підготовкою змін у репозиторії.\n\n"
        "Що я вмію:\n"
        "- робити review існуючого коду\n"
        "- готувати drafts для точкових змін у функціях і файлах\n"
        "- формувати change set для нових helper/module змін\n"
        "- готувати spec перед реалізацією\n\n"
        "Приклади запитів:\n"
        "- /review review existing search_in_repo implementation in repo_tools\n"
        "- /drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py\n"
        "- /drafts add input validation logging to read_file_range in tools/repo_tools.py\n"
        "- /changes create helper to export repo manifest summary as markdown\n"
        "- /spec підготуй специфікацію для додавання більш детального логування в search_in_repo\n\n"
        "Важливо:\n"
        "для складних функцій я можу повернути safe fallback suggestion замість ризикованого rewrite."
    )


async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message or not message.text:
        return

    user_text = message.text.strip()
    if not user_text:
        return

    try:
        result = run_root_agent(user_text)
        answer = (result.output_text or "").strip()

        if not answer:
            answer = "Агент повернув порожню відповідь."

        for chunk in _chunk_text(answer):
            await message.reply_text(chunk)

    except Exception as e:
        await message.reply_text(f"Помилка: {e}")


async def task(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message:
        return

    query = " ".join(context.args).strip()

    if not query:
        await message.reply_text("Використання: /task <текст|Jira key|Jira URL>")
        return

    try:
        result = run_task_intake(query)
        answer = (result.output_text or "").strip()

        if not answer:
            answer = "Не вдалося побудувати task brief."

        for chunk in _chunk_text(answer):
            await message.reply_text(chunk)

    except Exception as e:
        await message.reply_text(f"Помилка при обробці task intake: {e}")


async def brief(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message:
        return

    query = " ".join(context.args).strip()

    if not query:
        await message.reply_text("Використання: /brief <текст|Jira key|Jira URL>")
        return

    generated_file_path = ""

    try:
        result = run_brief_export_from_task_brief(query)
        generated_file_path = str(result.metadata.get("export_file_path") or "").strip()

        if not generated_file_path or not os.path.exists(generated_file_path):
            await message.reply_text("Не вдалося підготувати brief файл.")
            return

        with open(generated_file_path, "rb") as file_obj:
            await message.reply_document(
                document=file_obj,
                filename="brief.md",
            )

    except Exception as e:
        await message.reply_text(f"Помилка при побудові brief: {e}")

    finally:
        if generated_file_path and os.path.exists(generated_file_path):
            try:
                os.remove(generated_file_path)
            except OSError:
                pass


async def spec(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message:
        return

    query = " ".join(context.args).strip()

    if not query:
        await message.reply_text("Використання: /spec <текст|Jira key|Jira URL>")
        return

    generated_file_path = ""

    try:
        result = run_spec_export_from_task_brief(query)
        generated_file_path = str(result.metadata.get("export_file_path") or "").strip()

        if not generated_file_path or not os.path.exists(generated_file_path):
            await message.reply_text("Не вдалося підготувати spec файл.")
            return

        with open(generated_file_path, "rb") as file_obj:
            await message.reply_document(
                document=file_obj,
                filename="spec.md",
            )

    except Exception as e:
        await message.reply_text(f"Помилка при побудові spec: {e}")

    finally:
        if generated_file_path and os.path.exists(generated_file_path):
            try:
                os.remove(generated_file_path)
            except OSError:
                pass


async def reviewspec(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message:
        return

    query = " ".join(context.args).strip()

    if not query:
        await message.reply_text("Використання: /reviewspec <текст|Jira key|Jira URL>")
        return

    try:
        result = run_review_spec_from_task_brief(query)
        answer = (result.output_text or "").strip()

        if not answer:
            answer = "Не вдалося виконати review spec."

        for chunk in _chunk_text(answer):
            await message.reply_text(chunk)

    except Exception as e:
        await message.reply_text(f"Помилка при review spec: {e}")


async def report(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.effective_message

    if not message:
        return

    query = " ".join(context.args).strip()

    if not query:
        await message.reply_text("Використання: /report <запит>")
        return

    temp_file_path = ""

    try:
        result = run_root_agent(query)
        answer = (result.output_text or "").strip()

        if not answer:
            answer = "Агент повернув порожню відповідь."

        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            suffix=".txt",
            prefix="report_",
            delete=False,
        ) as temp_file:
            temp_file.write(answer)
            temp_file_path = temp_file.name

        with open(temp_file_path, "rb") as file_obj:
            await message.reply_document(
                document=file_obj,
                filename="report.txt",
            )

    except Exception as e:
        await message.reply_text(f"Помилка при формуванні звіту: {e}")

    finally:
        if temp_file_path and os.path.exists(temp_file_path):
            try:
                os.remove(temp_file_path)
            except OSError:
                pass


def main() -> None:
    print("Starting Telegram bot...")
    print(f"Token configured: {bool(settings.telegram_bot_token)}")

    app = Application.builder().token(settings.telegram_bot_token).build()

    app.add_handler(CommandHandler("start", start))
    app.add_handler(CommandHandler("task", task))
    app.add_handler(CommandHandler("brief", brief))
    app.add_handler(CommandHandler("spec", spec))
    app.add_handler(CommandHandler("reviewspec", reviewspec))
    app.add_handler(CommandHandler("report", report))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message))

    print("Telegram root bot started...")
    app.run_polling(drop_pending_updates=True)


if __name__ == "__main__":
    main()
