from __future__ import annotations

import os
import unittest
from types import SimpleNamespace
from unittest.mock import patch

import services.bitbucket_credentials as bitbucket_credentials_module
from services.bitbucket_credentials import BitbucketCredentialResolver


class BitbucketCredentialAliasResolverTests(unittest.TestCase):
    def setUp(self) -> None:
        self.resolver = BitbucketCredentialResolver()

    def test_global_fallback_still_works_unchanged(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="global-read-token",
            bitbucket_api_token="",
            bitbucket_username="",
            bitbucket_app_password="",
        )
        with patch.dict(os.environ, {}, clear=False), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(repo_id="catalog_service")

        self.assertEqual(credentials.username, "x-token-auth")
        self.assertEqual(credentials.secret, "global-read-token")
        self.assertTrue(credentials.used_global_fallback)
        self.assertEqual(credentials.auth_mode_used, "token")

    def test_alias_specific_token_overrides_global(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="global-read-token",
            bitbucket_api_token="",
            bitbucket_username="",
            bitbucket_app_password="",
        )
        with patch.dict(os.environ, {"BITBUCKET_REPO_TOKEN__CATALOG_TEST": "alias-token"}, clear=False), patch.object(
            bitbucket_credentials_module.settings,
            "runtime",
            fake_runtime,
        ):
            credentials = self.resolver.resolve(repo_id="catalog_service", credential_alias="catalog-test")

        self.assertEqual(credentials.secret, "alias-token")
        self.assertEqual(credentials.resolved_credential_alias, "CATALOG_TEST")
        self.assertFalse(credentials.used_global_fallback)

    def test_alias_specific_username_and_password_override_global(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="",
            bitbucket_api_token="",
            bitbucket_username="global-user",
            bitbucket_app_password="global-pass",
        )
        with patch.dict(
            os.environ,
            {"BITBUCKET_USERNAME__CALL_TEST": "alias-user", "BITBUCKET_APP_PASSWORD__CALL_TEST": "alias-pass"},
            clear=False,
        ), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(repo_id="call_service", credential_alias="call-test")

        self.assertEqual(credentials.username, "alias-user")
        self.assertEqual(credentials.secret, "alias-pass")
        self.assertEqual(credentials.auth_mode_used, "basic")

    def test_safe_metadata_never_exposes_secret_values(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="global-secret-token",
            bitbucket_api_token="",
            bitbucket_username="",
            bitbucket_app_password="",
        )
        with patch.dict(os.environ, {}, clear=False), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(repo_id="catalog_service")

        metadata = credentials.safe_metadata()
        self.assertNotIn("secret", "".join(str(value) for value in metadata.values()).lower())
        self.assertEqual(metadata["auth_mode_used"], "token")


if __name__ == "__main__":
    unittest.main()
