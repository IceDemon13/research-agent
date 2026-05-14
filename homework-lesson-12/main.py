"""CLI entry point for Homework 12.

Usage:
    python main.py                            # interactive REPL
    python main.py --topic "..." --quick      # one-shot run, skip HITL save
    python main.py --batch                    # run 5 pre-baked topics back-to-back
                                              # (perfect for generating traces
                                              #  for the screenshots)

The supervisor's run() creates one Langfuse trace per call, all grouped under
the same session_id when run within a single CLI invocation.
"""
from __future__ import annotations

import argparse
from uuid import uuid4

from config import settings
from langfuse_setup import flush, is_enabled
from supervisor import HomeworkSupervisor, SupervisorResult


# Pre-baked topics for the screenshot batch run. They are intentionally
# varied (technical, social, scientific, business, historical) to make the
# trace list look real in the screenshots.
DEFAULT_BATCH_TOPICS = [
    "Retrieval-Augmented Generation: trade-offs vs. long-context LLMs",
    "Чи замінять AI-агенти класичні SaaS-додатки до 2030 року?",
    "Energy efficiency of small language models on edge devices",
    "Best practices for evaluating multi-agent systems in production",
    "Як LLM-as-a-Judge змінює QA-процеси в AI-командах",
]


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
            "args": {"title": new_title, "content": new_content},
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
            reason = (
                input("Reason for rejection: ").strip()
                or "User rejected saving the report."
            )
            resumed = supervisor.resume_save(thread_id, {"type": "reject", "message": reason})
            print("\nSave rejected.")
            print(resumed)
            return
        print("Please enter approve, edit, or reject.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Homework 12 multi-agent CLI with Langfuse")
    parser.add_argument("--topic", type=str, help="Run a single topic non-interactively.")
    parser.add_argument(
        "--quick",
        action="store_true",
        help="Skip HITL save review (only relevant with --topic / --batch).",
    )
    parser.add_argument(
        "--batch",
        action="store_true",
        help="Run a set of pre-baked topics back-to-back (great for generating screenshots).",
    )
    parser.add_argument(
        "--session-id",
        type=str,
        default=None,
        help="Override the session_id propagated to Langfuse.",
    )
    parser.add_argument(
        "--user-id",
        type=str,
        default=None,
        help="Override the user_id propagated to Langfuse.",
    )
    parser.add_argument(
        "--tag",
        action="append",
        default=None,
        help="Additional trace tag (can be passed multiple times).",
    )
    return parser.parse_args()


def banner() -> None:
    print("=" * 70)
    print("Homework Lesson 12 — Multi-Agent Research Assistant + Langfuse")
    print("=" * 70)
    if is_enabled():
        print(f"Langfuse: enabled  ({settings.langfuse_host})")
    else:
        print(
            "Langfuse: DISABLED — set LANGFUSE_PUBLIC_KEY / LANGFUSE_SECRET_KEY "
            "in .env and re-run.\n"
            "  Without keys the code still runs but no traces/scores are sent."
        )
    print()


def run_one(supervisor: HomeworkSupervisor, topic: str, args: argparse.Namespace, session_id: str) -> SupervisorResult:
    result = supervisor.run(
        topic,
        session_id=session_id,
        user_id=args.user_id or settings.default_user_id,
        extra_tags=args.tag or [],
    )
    print_plan(result)
    print("\n=== REPORT ===\n")
    print(result.report_markdown)
    print_critique(result)
    if not args.quick:
        try:
            handle_save_review(supervisor, result)
        except EOFError:
            print("[main] No interactive input — skipping save review.")
    return result


def main() -> None:
    args = parse_args()
    banner()
    supervisor = HomeworkSupervisor()
    session_id = args.session_id or f"{settings.default_session_prefix}-{uuid4().hex[:8]}"
    print(f"Session: {session_id}")
    print(f"User:    {args.user_id or settings.default_user_id}\n")

    try:
        if args.batch:
            for topic in DEFAULT_BATCH_TOPICS:
                print(f"\n>>> Batch topic: {topic}")
                run_one(supervisor, topic, args, session_id)
            return
        if args.topic:
            run_one(supervisor, args.topic, args, session_id)
            return

        print("Enter a research topic, or type /quit to exit.")
        while True:
            topic = input("\nTopic> ").strip()
            if not topic:
                continue
            if topic.lower() in {"/quit", "quit", "exit"}:
                break
            run_one(supervisor, topic, args, session_id)
    finally:
        flush()


if __name__ == "__main__":
    main()
