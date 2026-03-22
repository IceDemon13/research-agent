import unittest

from services.i18n_service import I18nService


class I18nServiceTests(unittest.TestCase):
    def test_missing_key_falls_back_to_key(self) -> None:
        service = I18nService()
        self.assertEqual(service.t("uk", "missing.key.example"), "missing.key.example")


if __name__ == "__main__":
    unittest.main()
