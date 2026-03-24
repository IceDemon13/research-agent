import unittest
from unittest.mock import patch

from agents.spec_agent import format_spec_for_ui, run_spec_agent
from contracts.spec_contract import SpecContract


class SpecAgentTests(unittest.TestCase):
    def test_run_spec_agent_returns_structured_engineering_spec(self) -> None:
        model_output = """# Відображення типів бонусів в історії

Summary:
Потрібно показувати тип бонусу в історії операцій, щоб користувач розумів природу нарахування або списання.

## Functional Requirements
- Історія бонусів має показувати тип бонусу для кожного запису.
- Якщо тип бонусу відсутній, інтерфейс має показувати нейтральне значення без помилки.

## Backend Changes
- Розширити дані історії бонусів полем типу бонусу.
- Оновити мапінг даних для відповіді, що використовується історією.

## Frontend Changes
- Додати відображення типу бонусу в рядку історії.

## Acceptance Criteria
- Користувач бачить тип бонусу в кожному записі історії, де він доступний.
- Для записів без типу бонусу інтерфейс відображається без помилки.

## Risks
- Може знадобитися узгодження назв типів бонусів між джерелами даних.

## Open Questions
- Чи потрібна локалізація назв типів бонусів?
"""
        with patch("agents.spec_agent.ensure_repo_context", return_value={"chunks": [], "files_used": []}), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, []),
        ):
            result = run_spec_agent("додати відображення типів бонусів в історії")

        spec = result.metadata["spec"]
        self.assertEqual(spec.title, "Відображення типів бонусів в історії")
        self.assertTrue(spec.functional_requirements)
        self.assertTrue(spec.backend_changes)
        self.assertTrue(spec.frontend_changes)
        self.assertTrue(spec.acceptance_criteria)
        self.assertTrue(spec.open_questions)
        self.assertIn("## Functional Requirements", result.output_text)
        self.assertIn("## Backend Changes", result.output_text)
        self.assertIn("## Frontend Changes", result.output_text)
        self.assertEqual(spec.scope, spec.functional_requirements)
        self.assertTrue(spec.requirements)

    def test_run_spec_agent_sanitizes_acceptance_criteria_and_infers_missing_sections(self) -> None:
        model_output = """# Оновити бонусну історію

Summary:
Потрібно змінити відображення історії бонусів.

## Functional Requirements
- Історія має містити додаткову інформацію про бонус.

## Acceptance Criteria
- Перевірити repo context і релевантні файли.

## Risks
- Можлива різниця в даних між старими та новими записами.
"""
        with patch("agents.spec_agent.ensure_repo_context", return_value={"chunks": [], "files_used": []}), patch(
            "agents.spec_agent.run_react_loop",
            return_value=(model_output, []),
        ):
            result = run_spec_agent("додати відображення типів бонусів в історії")

        spec = result.metadata["spec"]
        self.assertTrue(spec.backend_changes)
        self.assertTrue(spec.frontend_changes)
        self.assertTrue(spec.open_questions)
        self.assertTrue(spec.acceptance_criteria)
        self.assertFalse(any("repo context" in item.lower() for item in spec.acceptance_criteria))
        self.assertFalse(any("file" in item.lower() for item in spec.acceptance_criteria))

    def test_format_spec_for_ui_uses_deterministic_sections(self) -> None:
        spec = SpecContract(
            title="Відображення типів бонусів в історії",
            summary="Потрібно показати тип бонусу в історії операцій.",
            functional_requirements=["Показувати тип бонусу в історії."],
            backend_changes=["Додати тип бонусу в дані історії."],
            frontend_changes=["Відобразити тип бонусу в UI."],
            acceptance_criteria=["Користувач бачить тип бонусу в історії."],
            risks=["Потрібна перевірка сумісності старих записів."],
            open_questions=["Чи потрібна локалізація назв типів?"],
        )

        formatted = format_spec_for_ui(spec)

        self.assertTrue(formatted.startswith("# Відображення типів бонусів в історії"))
        self.assertIn("Summary:", formatted)
        self.assertIn("## Functional Requirements", formatted)
        self.assertIn("## Acceptance Criteria", formatted)
        self.assertIn("## Open Questions", formatted)


if __name__ == "__main__":
    unittest.main()
