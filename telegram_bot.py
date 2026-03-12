from telegram import Update
from telegram.ext import Application, CommandHandler, ContextTypes, MessageHandler, filters

from agent import build_agent
from config import settings


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text(
        "Привіт. Я research agent.\n"
        "Можу шукати інформацію в інтернеті, читати сторінки та зберігати звіт у файл.\n\n"
        "Приклад:\n"
        "Знайди інформацію про LangGraph і збережи звіт у report.md"
    )


async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_text = update.message.text.strip()

    try:
        # новий агент на кожне повідомлення
        agent = build_agent()

        result = agent.invoke(
            {
                "messages": [
                    {
                        "role": "user",
                        "content": user_text
                    }
                ]
            }
        )

        result_messages = result.get("messages", [])
        if not result_messages:
            await update.message.reply_text("Не вдалося отримати відповідь від агента.")
            return

        final_message = result_messages[-1]
        answer = str(final_message.content).strip()

        if not answer:
            answer = "Агент відпрацював, але повернув порожню відповідь."

        # Telegram має ліміт на довжину повідомлення
        if len(answer) > 4000:
            for i in range(0, len(answer), 4000):
                await update.message.reply_text(answer[i:i + 4000])
        else:
            await update.message.reply_text(answer)

    except Exception as e:
        await update.message.reply_text(f"Помилка: {e}")


def main():
    app = Application.builder().token(settings.telegram_bot_token).build()

    app.add_handler(CommandHandler("start", start))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message))

    print("Telegram bot started...")
    app.run_polling()


if __name__ == "__main__":
    main()