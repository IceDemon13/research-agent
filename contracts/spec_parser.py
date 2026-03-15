from __future__ import annotations

from contracts.spec_contract import SpecContract


SECTION_ALIASES = {
    "title": ["# spec", "# специфікація", "# spec:"],
    "goal": ["## 1. мета", "## мета"],
    "context": ["## 2. проблема / контекст", "## проблема / контекст", "## контекст"],
    "scope": ["## 3. scope", "## scope"],
    "out_of_scope": ["## 4. out of scope", "## out of scope"],
    "requirements": ["## 5. основні вимоги", "## основні вимоги", "## вимоги"],
    "acceptance_criteria": ["## 6. acceptance criteria", "## acceptance criteria"],
    "risks": ["## 7. ризики / відкриті питання", "## ризики / відкриті питання", "## ризики"],
}


def parse_spec_text(text: str) -> SpecContract:
    lines = [line.rstrip() for line in text.splitlines()]
    sections: dict[str, list[str]] = {
        "title": [],
        "goal": [],
        "context": [],
        "scope": [],
        "out_of_scope": [],
        "requirements": [],
        "acceptance_criteria": [],
        "risks": [],
    }

    current_section: str | None = None

    for raw_line in lines:
        line = raw_line.strip()
        if not line:
            continue

        lowered = line.lower()

        matched_section = _match_section(lowered)
        if matched_section:
            current_section = matched_section
            if matched_section == "title":
                sections["title"].append(line)
            continue

        if current_section:
            sections[current_section].append(line)

    return SpecContract(
        title=_join_text(sections["title"]).replace("# ", "").strip(),
        goal=_join_text(sections["goal"]),
        context=_join_text(sections["context"]),
        scope=_normalize_list(sections["scope"]),
        out_of_scope=_normalize_list(sections["out_of_scope"]),
        requirements=_normalize_list(sections["requirements"]),
        acceptance_criteria=_normalize_list(sections["acceptance_criteria"]),
        risks=_normalize_list(sections["risks"]),
    )


def _match_section(line: str) -> str | None:
    for section_name, variants in SECTION_ALIASES.items():
        if any(line.startswith(variant) for variant in variants):
            return section_name
    return None


def _join_text(lines: list[str]) -> str:
    return " ".join(line.strip() for line in lines).strip()


def _normalize_list(lines: list[str]) -> list[str]:
    items: list[str] = []

    for line in lines:
        cleaned = line.strip()

        for prefix in ("- ", "* ", "• "):
            if cleaned.startswith(prefix):
                cleaned = cleaned[len(prefix):].strip()
                break

        if cleaned:
            items.append(cleaned)

    return items