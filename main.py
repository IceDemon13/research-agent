from agent import build_agent


def main():
    agent = build_agent()
    messages = []

    print("Research Agent started. Type 'exit' to quit.")

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Goodbye!")
            break

        messages.append({"role": "user", "content": user_input})

        result = agent.invoke({"messages": messages})

        result_messages = result.get("messages", [])
        if not result_messages:
            print("\nAgent: No response received.")
            continue

        final_message = result_messages[-1]

        messages = []
        for msg in result_messages:
            if hasattr(msg, "type") and hasattr(msg, "content"):
                role = "assistant"
                if msg.type == "human":
                    role = "user"
                elif msg.type == "tool":
                    role = "tool"

                messages.append(
                    {
                        "role": role,
                        "content": str(msg.content),
                    }
                )

        print(f"\nAgent: {final_message.content}")


if __name__ == "__main__":
    main()