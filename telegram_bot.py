from telegram import Update
from telegram.ext import Application, CommandHandler, ContextTypes, MessageHandler, filters

from config import settings
from root_agent import run_root_agent


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text(
        "Привіт. Я root agent.\n"
        "Я можу маршрутизувати запити між різними агентами.\n\n"
        "Наприклад:\n"
        "1. Знайди інформацію про LangGraph і збережи звіт у report.md\n"
        "2. Поясни коротко, що таке root agent"
    )


async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    message = update.effective_message

    if not message or not message.text:
        return

    user_text = message.text.strip()

    try:
        result, route = run_root_agent(user_text)

        result_messages = result.get("messages", [])
        if not result_messages:
            await message.reply_text("Не вдалося отримати відповідь.")
            return

        final_message = result_messages[-1]
        answer = str(final_message.content).strip()

        if not answer:
            answer = "Агент повернув порожню відповідь."

        answer = f"[Route: {route}]\n\n{answer}"

        if len(answer) > 4000:
            for i in range(0, len(answer), 4000):
                await message.reply_text(answer[i:i + 4000])
        else:
            await message.reply_text(answer)

    except Exception as e:
        await message.reply_text(f"Помилка: {e}")


def main():
    app = Application.builder().token(settings.telegram_bot_token).build()

    app.add_handler(CommandHandler("start", start))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message))

    print("Telegram root bot started...")
    app.run_polling()


if __name__ == "__main__":
    main()