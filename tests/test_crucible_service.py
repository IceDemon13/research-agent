import json
import shutil
import unittest
import uuid
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

import services.crucible_service as crucible_module
from services.crucible_service import CrucibleService


class CrucibleServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"crucible-service-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "crucible" / "reviews.json"

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_create_review_success(self) -> None:
        fake_runtime = SimpleNamespace(
            crucible_base_url="https://crucible.example.invalid",
            crucible_username="ci-user",
            crucible_password="app-password",
            crucible_api_token="",
            crucible_project_key="CR-PROJ",
            crucible_review_registry_path=str(self.registry_path),
        )
        create_response = MagicMock()
        create_response.read.return_value = json.dumps(
            {"permaId": {"id": "CR-PROJ-1"}}
        ).encode("utf-8")
        create_response.headers = {"Location": "https://crucible.example.invalid/rest-service/reviews-v1/CR-PROJ-1"}
        create_response.__enter__.return_value = create_response
        create_response.__exit__.return_value = False

        reviewer_response = MagicMock()
        reviewer_response.read.return_value = b"{}"
        reviewer_response.headers = {}
        reviewer_response.__enter__.return_value = reviewer_response
        reviewer_response.__exit__.return_value = False

        with patch("services.crucible_service.request.urlopen", side_effect=[create_response, reviewer_response]) as mocked_urlopen, patch.object(
            crucible_module.settings,
            "runtime",
            fake_runtime,
        ):
            result = CrucibleService(storage_path=self.registry_path).create_review(
                "sample-repo",
                "feature/ai/update-run-output-20260320093000",
                "AI Review: Update run output",
                "Review body",
                ["alice", "bob"],
            )

        self.assertTrue(result.success)
        self.assertEqual(result.review_id, "CR-PROJ-1")
        self.assertTrue(result.url.endswith("/CR-PROJ-1"))
        self.assertEqual(mocked_urlopen.call_count, 2)
        create_request = mocked_urlopen.call_args_list[0].args[0]
        create_payload = json.loads(create_request.data.decode("utf-8"))
        self.assertEqual(create_payload["reviewData"]["projectKey"], "CR-PROJ")
        self.assertEqual(create_payload["reviewData"]["name"], "AI Review: Update run output")
        reviewer_request = mocked_urlopen.call_args_list[1].args[0]
        reviewer_payload = json.loads(reviewer_request.data.decode("utf-8"))
        self.assertEqual(
            reviewer_payload["reviewer"],
            [{"userName": "alice"}, {"userName": "bob"}],
        )

    def test_create_review_skips_duplicate_branch(self) -> None:
        fake_runtime = SimpleNamespace(
            crucible_base_url="https://crucible.example.invalid",
            crucible_username="ci-user",
            crucible_password="app-password",
            crucible_api_token="",
            crucible_project_key="CR-PROJ",
            crucible_review_registry_path=str(self.registry_path),
        )
        create_response = MagicMock()
        create_response.read.return_value = json.dumps(
            {"permaId": {"id": "CR-PROJ-1"}}
        ).encode("utf-8")
        create_response.headers = {"Location": "https://crucible.example.invalid/rest-service/reviews-v1/CR-PROJ-1"}
        create_response.__enter__.return_value = create_response
        create_response.__exit__.return_value = False

        with patch("services.crucible_service.request.urlopen", return_value=create_response) as mocked_urlopen, patch.object(
            crucible_module.settings,
            "runtime",
            fake_runtime,
        ):
            service = CrucibleService(storage_path=self.registry_path)
            first_result = service.create_review(
                "sample-repo",
                "feature/ai/update-run-output-20260320093000",
                "AI Review: Update run output",
                "Review body",
                [],
            )
            second_result = service.create_review(
                "sample-repo",
                "feature/ai/update-run-output-20260320093000",
                "AI Review: Update run output",
                "Review body",
                [],
            )

        self.assertTrue(first_result.success)
        self.assertTrue(second_result.success)
        self.assertTrue(second_result.duplicate)
        self.assertEqual(mocked_urlopen.call_count, 1)


if __name__ == "__main__":
    unittest.main()
