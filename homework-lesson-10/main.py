from __future__ import annotations

from supervisor import HomeworkSupervisor, SupervisorResult


def print_plan(result: SupervisorResult) -> None:
    print("\n=== PLAN ===")
    print(f"Objective: {result.plan.objective}")
    if result.plan.key_questions:
        print("Key questions:")
        for item in result.plan.key_questions:
            print(f"- {item}")


def print_critique(result: SupervisorResult) -> None:
    print("\n=== CRITIQUE ===")
    print(f"Verdict: {result.critique.verdict}")
    print(f"Summary: {result.critique.summary}")
    if result.critique.issues:
        print("Issues:")
        for item in result.critique.issues:
            print(f"- {item}")


def extract_interrupt_payload(response) -> dict | None:
    interrupts = getattr(response, "interrupts", ()) or ()
    if not interrupts:
        return None
    first = interrupts[0]
    return getattr(first, "value", None)


def prompt_edit_decision(original_title: str, original_content: str) -> dict:
    print("New title (Enter to keep current):")
    new_title = input("> ").strip() or original_title
    print("Paste replacement markdown. Finish with a single line: END")
    lines: list[str] = []
    while True:
        line = input()
        if line.strip() == "END":
            break
        lines.append(line)
    new_content = "\n".join(lines).strip() or original_content
    return {
        "type": "edit",
        "edited_action": {
            "name": "save_report",
            "args": {
                "title": new_title,
                "content": new_content,
            },
        },
    }


def handle_save_review(supervisor: HomeworkSupervisor, result: SupervisorResult) -> None:
    response, thread_id = supervisor.request_save(result)
    payload = extract_interrupt_payload(response)

    if payload is None:
        print("\nNo HITL interrupt was raised. Save agent returned immediately.")
        print(response)
        return

    action = payload["action_requests"][0]
    print("\n=== SAVE REVIEW ===")
    print(action["description"])
    print("\nType one of: approve | edit | reject")

    while True:
        decision_type = input("> ").strip().lower()
        if decision_type == "approve":
            resumed = supervisor.resume_save(thread_id, {"type": "approve"})
            print("\nSave approved.")
            print(resumed)
            return
        if decision_type == "edit":
            decision = prompt_edit_decision(result.topic, result.report_markdown)
            resumed = supervisor.resume_save(thread_id, decision)
            print("\nSave edited and executed.")
            print(resumed)
            return
        if decision_type == "reject":
            reason = input("Reason for rejection: ").strip() or "User rejected saving the report."
            resumed = supervisor.resume_save(thread_id, {"type": "reject", "message": reason})
            print("\nSave rejected.")
            print(resumed)
            return
        print("Please enter approve, edit, or reject.")


def main() -> None:
    supervisor = HomeworkSupervisor()
    print("Homework Lesson 10 REPL")
    print("Enter a research topic, or type /quit to exit.")

    while True:
        topic = input("\nTopic> ").strip()
        if not topic:
            continue
        if topic.lower() in {"/quit", "quit", "exit"}:
            break

        result = supervisor.run(topic)
        print_plan(result)
        print("\n=== REPORT ===\n")
        print(result.report_markdown)
        print_critique(result)
        handle_save_review(supervisor, result)


if __name__ == "__main__":
    main()
