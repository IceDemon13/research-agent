from root_agent import run_root_agent


def main():
    print("Root Agent started. Type 'exit' to quit.")

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Goodbye!")
            break

        result, route = run_root_agent(user_input)

        result_messages = result.get("messages", [])
        if not result_messages:
            print("\nAgent: No response received.")
            continue

        final_message = result_messages[-1]
        print(f"\n[Route: {route}]")
        print(f"Agent: {final_message.content}")


if __name__ == "__main__":
    main()