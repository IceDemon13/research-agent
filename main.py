from research_agent import ResearchAgentSession


def main():
    print("Research Agent started. Type 'exit' to quit.")

    agent = ResearchAgentSession()

    while True:
        user_input = input("\nYou: ").strip()

        if user_input.lower() in {"exit", "quit"}:
            print("Goodbye!")
            break

        print("\n--- AGENT START ---")
        answer = agent.run(user_input)
        print(f"\nAgent: {answer}")
        print("\n--- AGENT END ---")


if __name__ == "__main__":
    main()