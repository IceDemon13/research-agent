from __future__ import annotations

import re


CURRENT_MARKERS = (
    "зараз",
    "сьогодні",
    "на даний момент",
    "current",
    "today",
    "right now",
)


def sanitize_search_query(user_input: str, query: str) -> str:
    user_lower = user_input.lower()
    query_clean = query.strip()

    if any(marker in user_lower for marker in CURRENT_MARKERS):
        query_clean = re.sub(r"\b(19|20)\d{2}\b", "", query_clean)
        query_clean = re.sub(r"\s{2,}", " ", query_clean).strip()

    return query_clean