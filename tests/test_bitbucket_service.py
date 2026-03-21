import json
import unittest
from base64 import b64encode
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

import services.bitbucket_service as bitbucket_module
from services.bitbucket_service import BitbucketService


class BitbucketServiceTests(unittest.TestCase):
    def test_create_pull_request_success(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_api_base_url="https://api.bitbucket.org/2.0",
            bitbucket_username="ci-user",
            bitbucket_app_password="app-password",
            bitbucket_api_token="",
        )
        response = MagicMock()
        response.read.return_value = json.dumps(
            {
                "links": {
                    "html": {
                        "href": "https://bitbucket.org/acme/sample-repo/pull-requests/1",
                    }
                }
            }
        ).encode("utf-8")
        response.__enter__.return_value = response
        response.__exit__.return_value = False

        with patch("services.bitbucket_service.request.urlopen", return_value=response) as mocked_urlopen, patch.object(
            bitbucket_module.settings,
            "runtime",
            fake_runtime,
        ):
            result = BitbucketService().create_pull_request(
                "https://bitbucket.org/acme/sample-repo.git",
                "feature/ai/update-run-output-20260319213045",
                "main",
                "AI: Update run output",
                "Summary body",
            )

        self.assertTrue(result.success)
        self.assertEqual(
            result.url,
            "https://bitbucket.org/acme/sample-repo/pull-requests/1",
        )
        request_object = mocked_urlopen.call_args.args[0]
        self.assertIn(
            "/repositories/acme/sample-repo/pullrequests",
            request_object.full_url,
        )
        payload = json.loads(request_object.data.decode("utf-8"))
        self.assertEqual(payload["title"], "AI: Update run output")
        self.assertEqual(payload["source"]["branch"]["name"], "feature/ai/update-run-output-20260319213045")
        self.assertEqual(payload["destination"]["branch"]["name"], "main")

    def test_create_pull_request_requires_credentials(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_api_base_url="https://api.bitbucket.org/2.0",
            bitbucket_repo_token="",
            bitbucket_username="",
            bitbucket_app_password="",
            bitbucket_api_token="",
        )

        with patch.object(
            bitbucket_module.settings,
            "runtime",
            fake_runtime,
        ):
            result = BitbucketService().create_pull_request(
                "https://bitbucket.org/acme/sample-repo.git",
                "feature/ai/update-run-output-20260319213045",
                "main",
                "AI: Update run output",
                "Summary body",
            )

        self.assertFalse(result.success)
        self.assertIn("credentials", result.error.lower())

    def test_create_pull_request_prefers_token_auth_header(self) -> None:
        fake_runtime = SimpleNamespace(
            bitbucket_api_base_url="https://api.bitbucket.org/2.0",
            bitbucket_repo_token="repo-token",
            bitbucket_username="ci-user",
            bitbucket_app_password="app-password",
            bitbucket_api_token="api-token",
        )
        response = MagicMock()
        response.read.return_value = json.dumps({"links": {"html": {"href": ""}}}).encode("utf-8")
        response.__enter__.return_value = response
        response.__exit__.return_value = False

        with patch("services.bitbucket_service.request.urlopen", return_value=response) as mocked_urlopen, patch.object(
            bitbucket_module.settings,
            "runtime",
            fake_runtime,
        ):
            BitbucketService().create_pull_request(
                "https://bitbucket.org/acme/sample-repo.git",
                "feature/ai/run-1",
                "main",
                "AI: Update run output",
                "Summary body",
            )

        request_object = mocked_urlopen.call_args.args[0]
        expected_header = "Basic " + b64encode(b"x-token-auth:repo-token").decode("ascii")
        self.assertEqual(request_object.headers["Authorization"], expected_header)


if __name__ == "__main__":
    unittest.main()
