from __future__ import annotations

import json
from pathlib import Path
from threading import Lock


DEFAULT_LOCALE = "uk"
SUPPORTED_LOCALES = ("uk", "en")


class I18nService:
    def __init__(self, *, catalogs_dir: Path | None = None) -> None:
        self._catalogs_dir = catalogs_dir or (Path(__file__).resolve().parent.parent / "static" / "i18n")
        self._catalogs: dict[str, dict[str, str]] = {}
        self._lock = Lock()

    def t(self, locale: str, key: str, **variables: object) -> str:
        resolved_locale = self.normalize_locale(locale)
        template = (
            self._catalog(resolved_locale).get(key)
            or self._catalog("en").get(key)
            or self._catalog(DEFAULT_LOCALE).get(key)
            or key
        )
        if not variables:
            return template
        try:
            return template.format(**{name: str(value) for name, value in variables.items()})
        except Exception:
            return template

    def normalize_locale(self, locale: str | None) -> str:
        resolved = str(locale or "").strip().lower()
        if resolved in SUPPORTED_LOCALES:
            return resolved
        return DEFAULT_LOCALE

    def _catalog(self, locale: str) -> dict[str, str]:
        resolved = self.normalize_locale(locale)
        with self._lock:
            cached = self._catalogs.get(resolved)
            if cached is not None:
                return cached
            path = self._catalogs_dir / f"{resolved}.json"
            if path.exists():
                loaded = json.loads(path.read_text(encoding="utf-8"))
                self._catalogs[resolved] = {
                    str(key or "").strip(): str(value or "")
                    for key, value in dict(loaded or {}).items()
                    if str(key or "").strip()
                }
            else:
                self._catalogs[resolved] = {}
            return self._catalogs[resolved]
