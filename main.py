from agents.root_agent import run_root_agent


def main() -> None:
    print("Research Agent started. Type 'exit' to quit.")

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Bye!")
            break

        print("\n--- AGENT START ---")
        result = run_root_agent(user_input)
        print(f"\nAgent: {result.output_text}")
        print("\n--- AGENT END ---")


if __name__ == "__main__":
    main()