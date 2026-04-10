from __future__ import annotations

from schemas import SpecOutput
from supervisor import TeamDeliveryResult, TeamSupervisor


def print_section(title: str) -> None:
    print(f"\n=== {title} ===")


def print_spec(spec: SpecOutput) -> None:
    print_section("SPEC")
    print(f"Title: {spec.title}")
    print(f"Complexity: {spec.estimated_complexity}")
    if spec.requirements:
        print("Requirements:")
        for item in spec.requirements:
            print(f"- {item}")
    if spec.acceptance_criteria:
        print("Acceptance Criteria:")
        for item in spec.acceptance_criteria:
            print(f"- {item}")


def print_qa(result: TeamDeliveryResult) -> None:
    print_section("QA REVIEW")
    print(f"Verdict: {result.review.verdict}")
    print(f"Score: {result.review.score}")
    print(f"Iterations Used: {result.iterations_used}")
    print(f"Completed Successfully: {result.completed_successfully}")
    if result.review.issues:
        print("Issues:")
        for item in result.review.issues:
            print(f"- {item}")


def prompt_spec_feedback() -> str:
    print("Enter feedback for BA to regenerate the spec:")
    return input("> ").strip()


def spec_approval_gate(spec: SpecOutput) -> tuple[str, str | None]:
    print("\nApprove this spec before development?")
    print("Type one of: approve | edit | reject")
    while True:
        decision = input("> ").strip().lower()
        if decision == "approve":
            return "approve", None
        if decision == "edit":
            feedback = prompt_spec_feedback()
            if not feedback:
                print("Feedback is required for edit.")
                continue
            return "edit", feedback
        if decision == "reject":
            return "reject", None
        print("Please enter approve, edit, or reject.")


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
            "name": "save_final_report",
            "args": {
                "title": new_title,
                "content": new_content,
            },
        },
    }


def handle_save_review(supervisor: TeamSupervisor, result: TeamDeliveryResult) -> None:
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
            decision = prompt_edit_decision(result.spec.title, result.artifact_markdown)
            resumed = supervisor.resume_save(thread_id, decision)
            print("\nSave edited and executed.")
            print(resumed)
            return
        if decision_type == "reject":
            reason = input("Reason for rejection: ").strip() or "User rejected saving the team artifact."
            resumed = supervisor.resume_save(thread_id, {"type": "reject", "message": reason})
            print("\nSave rejected.")
            print(resumed)
            return
        print("Please enter approve, edit, or reject.")


def print_final_summary(result: TeamDeliveryResult, report_path: str) -> None:
    print_section("FINAL SUMMARY")
    print(f"Spec: {result.spec.title}")
    print(f"Complexity: {result.spec.estimated_complexity}")
    print("Created Files:")
    for item in result.code_output.files_created:
        print(f"- {item}")
    print(f"QA Verdict: {result.review.verdict}")
    print(f"Iterations: {result.iterations_used}")
    print(f"Report Saved: {report_path}")


def main() -> None:
    supervisor = TeamSupervisor()
    print("Diploma Dev Team Demo")
    print("Describe a user story, or type /quit to exit.")

    while True:
        user_story = input("\nUser Story> ").strip()
        if not user_story:
            continue
        if user_story.lower() in {"/quit", "quit", "exit"}:
            break

        spec_feedback: str | None = None
        while True:
            spec = supervisor.run(user_story, feedback=spec_feedback)
            print_spec(spec)
            action, feedback = spec_approval_gate(spec)
            if action == "approve":
                break
            if action == "reject":
                report_path = supervisor.save_rejection_report(user_story, spec)
                print_section("RUN STOPPED")
                print("Spec rejected by user. Run finished without code generation.")
                print(f"Report Saved: {report_path}")
                spec = None
                break
            spec_feedback = feedback

        if spec is None:
            continue

        result = supervisor.finalize(user_story, spec)
        print_section("ARTIFACT")
        print()
        print(result.artifact_markdown)
        print_qa(result)
        report_path = supervisor.save_result_report(result)
        print_final_summary(result, report_path)
        if not result.completed_successfully:
            print("\nRun stopped after the maximum number of revision iterations without QA approval.")
            continue


if __name__ == "__main__":
    main()
