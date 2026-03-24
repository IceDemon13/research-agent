from __future__ import annotations

import os
import unittest
from types import SimpleNamespace
from unittest.mock import patch

import services.bitbucket_credentials as bitbucket_credentials_module
from services.bitbucket_credentials import BitbucketCredentialResolver


class BitbucketCredentialResolverTests(unittest.TestCase):
    def setUp(self) -> None:
        self.resolver = BitbucketCredentialResolver()

    def test_repo_specific_credentials_override_shared_defaults(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="",
            bitbucket_api_token="",
            bitbucket_username="shared-user",
            bitbucket_app_password="shared-pass",
        )
        with patch.dict(
            os.environ,
            {
                "BITBUCKET_USERNAME__CATALOG_SERVICE": "repo-user",
                "BITBUCKET_APP_PASSWORD__CATALOG_SERVICE": "repo-pass",
            },
            clear=False,
        ), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(repo_id="catalog-service")

        self.assertEqual(credentials.username, "repo-user")
        self.assertEqual(credentials.secret, "repo-pass")
        self.assertEqual(credentials.source, "repo:CATALOG_SERVICE")
        self.assertEqual(credentials.auth_kind, "basic")

    def test_shared_credentials_work_when_repo_override_is_missing(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="",
            bitbucket_api_token="",
            bitbucket_username="shared-user",
            bitbucket_app_password="shared-pass",
        )
        with patch.dict(os.environ, {}, clear=False), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(repo_id="orders_service")

        self.assertEqual(credentials.username, "shared-user")
        self.assertEqual(credentials.secret, "shared-pass")
        self.assertEqual(credentials.source, "global")
        self.assertEqual(credentials.auth_kind, "basic")

    def test_workspace_token_overrides_global_credentials(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_repo_token="",
            bitbucket_api_token="",
            bitbucket_username="shared-user",
            bitbucket_app_password="shared-pass",
        )
        with patch.dict(
            os.environ,
            {
                "BITBUCKET_API_TOKEN__WORKSPACE__ACME": "workspace-token",
            },
            clear=False,
        ), patch.object(bitbucket_credentials_module.settings, "runtime", fake_runtime):
            credentials = self.resolver.resolve(remote_url="https://bitbucket.org/acme/catalog_service.git")

        self.assertEqual(credentials.username, "x-token-auth")
        self.assertEqual(credentials.secret, "workspace-token")
        self.assertEqual(credentials.source, "workspace:ACME")
        self.assertEqual(credentials.auth_kind, "token")


if __name__ == "__main__":
    unittest.main()
