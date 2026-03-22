import shutil
import unittest
import uuid
import json
import warnings
from pathlib import Path
from unittest.mock import patch

from fastapi.testclient import TestClient

from agents import root_agent
from contracts.agent_result import AgentResult
from contracts.actor_contract import ActorContext
from contracts.crucible_review_contract import CrucibleReviewResult
from contracts.diff_contract import DiffFile, DiffResult
from contracts.permission_contract import PermissionDecision, PermissionScope
from contracts.repo_index import RepoProfile
from contracts.repo_metadata import RepoMetadata
from contracts.run_detail_contract import RunDetail
from contracts.repo_onboarding_contract import RepoOnboardingResult
from contracts.run_contract import RunRecord
from services.run_service import RunService
from services.db_service import DatabaseService
from services.auth_service import AuthService
import web_app
from web_app import app


class WebAppTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(app)
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"web-app-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.storage_dir = self.workspace_root / "artifacts" / "runs"
        self.metadata_db_path = self.workspace_root / "artifacts" / "metadata.db"
        self.db_service = DatabaseService(dsn=f"sqlite:///{self.metadata_db_path}")
        self.auth_service = AuthService(db_service=self.db_service)
        self._db_patch = patch("web_app._db_service", return_value=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._header_fallback_patch = patch("web_app._allow_header_actor_fallback", return_value=True)
        self._db_patch.start()
        self._auth_patch.start()
        self._header_fallback_patch.start()

    def tearDown(self) -> None:
        self._header_fallback_patch.stop()
        self._auth_patch.stop()
        self._db_patch.stop()
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _repatch_auth_service(self) -> None:
        self._auth_patch.stop()
        self.auth_service = AuthService(db_service=self.db_service)
        self._auth_patch = patch("web_app._auth_service", return_value=self.auth_service)
        self._auth_patch.start()

    def _create_persisted_run(
        self,
        *,
        goal: str,
        mode: str,
        repo_id: str = "",
        status: str = "success",
        detail_payload: dict | None = None,
    ) -> RunRecord:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run(goal, repo_id=repo_id, actor_context=actor)
        finished_run = service.finish_run(run.run_id, status)
        payload = {"mode": mode, **dict(detail_payload or {})}
        service.persist_run_detail(
            run.run_id,
            payload,
            log_path=finished_run.log_path,
        )
        return finished_run

    def _load_persisted_run_detail(self, run_record: RunRecord) -> RunDetail:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        detail = service.load_run_detail(
            run_record.run_id,
            run_record=run_record,
            log_path=run_record.log_path,
        )
        self.assertIsNotNone(detail)
        return detail

    def test_health_endpoint(self) -> None:
        response = self.client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "ok")

    def test_run_detail_localizes_summary_by_requested_language(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No-op run", repo_id="sample", actor_context=actor)
        finished = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "run_outcome_type": "success_no_changes",
                "implementation_result": {"final_status": "no_changes"},
            },
            log_path=finished.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response_uk = self.client.get(
                f"/runs/{run.run_id}",
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "uk"},
            )
            response_en = self.client.get(
                f"/runs/{run.run_id}",
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"},
            )

        self.assertEqual(response_uk.status_code, 200)
        self.assertEqual(response_en.status_code, 200)
        self.assertEqual(response_uk.json()["run"]["run_outcome_type"], "success_no_changes")
        self.assertEqual(response_en.json()["run"]["run_outcome_type"], "success_no_changes")
        self.assertNotEqual(response_uk.json()["run"]["final_result_summary"], response_en.json()["run"]["final_result_summary"])

    def test_pre_review_workflow_localizes_decision_statement(self) -> None:
        run_record = RunRecord(
            run_id="workflow-run-1",
            goal="Pre-review flow",
            status="success",
            started_at="2026-03-22T09:00:00+00:00",
            finished_at="2026-03-22T09:05:00+00:00",
            repo_id="sample",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        detail = RunDetail(
            run_id="workflow-run-1",
            mode="review",
            goal="Pre-review flow",
            repo_id="sample",
            status="success",
            review_result={"status": "approved", "summary": "Review is ready.", "approved_files": ["src/app.py"]},
            implementation_result={"artifact_summary": {"files_count": 1, "file_paths": ["src/app.py"]}},
            diff_result={"diff_available": True, "files": [{"file_path": "src/app.py", "change_type": "modified"}]},
            repo_context_summary={"files_used": ["src/app.py"]},
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run_record, detail)), patch(
            "web_app._execute_tracked_api_run",
            return_value=run_record,
        ), patch("web_app._load_run_detail_for_record", return_value=detail):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-1", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "uk"},
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["result"]["verdict"], "ready_for_review")
        self.assertIn("review", payload["result"]["decision_statement"].lower())

    def test_pre_review_without_artifact_short_circuits_without_model_call(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app._find_latest_implementation_run_with_artifact",
            return_value=(None, None),
        ), patch("web_app.root_agent.run_root_agent") as mocked_run_root_agent:
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-404", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "pre_review")
        self.assertEqual(payload["result"]["verdict"], "blocked_insufficient_artifact")
        self.assertFalse(payload["result"]["ready_for_crucible"])
        mocked_run_root_agent.assert_not_called()
        run_record = service.load_run(payload["run_id"])
        self.assertIsNotNone(run_record)
        detail = service.load_run_detail(
            payload["run_id"],
            run_record=run_record,
            log_path=run_record.log_path,
        )
        self.assertIsNotNone(detail)
        self.assertEqual(detail.model_used, "")
        self.assertEqual(detail.source_stage, "deterministic")
        self.assertIn("no concrete implementation artifact", detail.routing_reason.lower())

    def test_tracked_workflow_persists_model_routing_metadata(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        routed_result = AgentResult(
            agent_name="spec",
            output_text="Structured task analysis complete.",
            success=True,
            task_intent="read",
            repo_context={"files_used": ["src/app.py"]},
            metadata={
                "model_used": "gpt-5.4-mini",
                "routing_reason": "Analyze-task workflow uses the light model by default.",
                "was_escalated": False,
                "source_stage": "initial",
                "estimated_prompt_size": 321,
            },
        )

        with patch("web_app.RunService", return_value=service), patch(
            "web_app.root_agent.run_root_agent",
            return_value=routed_result,
        ):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-200", "repo_id": ""},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        run_id = response.json()["run_id"]
        detail = service.load_run_detail(run_id, run_record=service.load_run(run_id), log_path=service.load_run(run_id).log_path)
        self.assertIsNotNone(detail)
        self.assertEqual(detail.model_used, "gpt-5.4-mini")
        self.assertEqual(detail.routing_reason, "Analyze-task workflow uses the light model by default.")
        self.assertFalse(detail.was_escalated)
        self.assertEqual(detail.source_stage, "initial")
        self.assertEqual(detail.estimated_prompt_size, 321)

    def test_ui_pages_are_served(self) -> None:
        root_response = self.client.get("/", follow_redirects=False)
        index_response = self.client.get("/ui/index.html")
        workflow_response = self.client.get("/ui/workflow.html")
        runs_response = self.client.get("/ui/runs.html")
        run_response = self.client.get("/ui/run.html")
        create_response = self.client.get("/ui/create.html")
        repos_response = self.client.get("/ui/repos.html")
        login_response = self.client.get("/ui/login.html")
        admin_response = self.client.get("/ui/admin/index.html")
        users_response = self.client.get("/ui/admin/users.html")
        roles_response = self.client.get("/ui/admin/roles.html")
        policies_response = self.client.get("/ui/admin/policies.html")
        styles_response = self.client.get("/ui/styles.css")

        self.assertEqual(root_response.status_code, 307)
        self.assertEqual(root_response.headers["location"], "/ui/index.html")
        self.assertEqual(index_response.status_code, 200)
        self.assertIn("TELEMART AI Delivery Workflows", index_response.text)
        self.assertIn("Проаналізувати Jira задачу", index_response.text)
        self.assertIn("Структурувати задачу з вільного тексту", index_response.text)
        self.assertIn("Побудувати план імплементації", index_response.text)
        self.assertIn("Перевірити готовність до review", index_response.text)
        self.assertIn("languageSelector", index_response.text)
        self.assertIn("Керування repo", repos_response.text)
        self.assertIn("Зареєстровані repo", repos_response.text)
        self.assertIn("repo.action.sync", repos_response.text)
        self.assertIn("repo.action.reindex", repos_response.text)
        self.assertEqual(workflow_response.status_code, 200)
        self.assertIn("Технічні деталі", workflow_response.text)
        self.assertIn("Запустити workflow", workflow_response.text)
        self.assertIn("Виправити проблеми і повторити", workflow_response.text)
        self.assertIn("Global blockers", workflow_response.text)
        self.assertIn("Evidence", workflow_response.text)
        self.assertIn("languageSelector", workflow_response.text)
        self.assertEqual(runs_response.status_code, 200)
        self.assertIn("Технічні run-и", runs_response.text)
        self.assertIn("Користувач", runs_response.text)
        self.assertIn("Почато", runs_response.text)
        self.assertIn("Гілка", runs_response.text)
        self.assertIn("languageSelector", runs_response.text)
        self.assertEqual(run_response.status_code, 200)
        self.assertIn("Публікація", run_response.text)
        self.assertIn("Результат", run_response.text)
        self.assertIn("Коренева причина", run_response.text)
        self.assertIn("Підсумок draft", run_response.text)
        self.assertIn("Validation", run_response.text)
        self.assertIn("Підсумок apply", run_response.text)
        self.assertIn("Diff", run_response.text)
        self.assertIn("Retry Context", run_response.text)
        self.assertIn("Previous Attempt Summary", run_response.text)
        self.assertIn("AI review comments", run_response.text)
        self.assertIn("Погодити", run_response.text)
        self.assertIn("Відхилити", run_response.text)
        self.assertIn("languageSelector", run_response.text)
        self.assertEqual(create_response.status_code, 200)
        self.assertIn("Create Run", create_response.text)
        self.assertIn("Technical Runs", create_response.text)
        self.assertEqual(repos_response.status_code, 200)
        self.assertIn("Керування repo", repos_response.text)
        self.assertIn("Підключити repo", repos_response.text)
        self.assertEqual(login_response.status_code, 200)
        self.assertIn("TELEMART AI Delivery Workflows", login_response.text)
        self.assertIn("Мова", login_response.text)
        self.assertEqual(admin_response.status_code, 200)
        self.assertIn("Loading admin data...", admin_response.text)
        self.assertIn("id=\"adminContent\" hidden", admin_response.text)
        self.assertIn("Адмін", admin_response.text)
        self.assertEqual(users_response.status_code, 200)
        self.assertIn("Loading admin data...", users_response.text)
        self.assertIn("id=\"adminContent\" hidden", users_response.text)
        self.assertIn("Створити користувача", users_response.text)
        self.assertEqual(roles_response.status_code, 200)
        self.assertIn("Loading admin data...", roles_response.text)
        self.assertIn("Ролі", roles_response.text)
        self.assertEqual(policies_response.status_code, 200)
        self.assertIn("Loading admin data...", policies_response.text)
        self.assertIn("Політики", policies_response.text)
        self.assertEqual(styles_response.status_code, 200)
        self.assertIn("--primary:", styles_response.text)
        self.assertIn(".severity-risk", styles_response.text)

    def _create_admin_user(self, *, username: str = "admin", password: str = "StrongPass123A!") -> None:
        self.auth_service.create_user(
            username=username,
            display_name="Admin",
            email="admin@example.com",
            role_name="admin",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password=password,
        )

    def _login(self, *, username: str = "admin", password: str = "StrongPass123A!") -> dict:
        response = self.client.post("/auth/login", json={"username": username, "password": password})
        self.assertEqual(response.status_code, 200)
        return response.json()

    def _insert_legacy_user(
        self,
        *,
        user_id: str,
        username: str,
        display_name: str,
        role: str,
        password_hash: str,
        is_active: bool = True,
    ) -> None:
        with self.db_service._connection() as connection:
            connection.cursor().execute(
                self.db_service._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """
                ),
                self.db_service._params(
                    user_id,
                    username,
                    display_name,
                    "",
                    "user",
                    role,
                    "",
                    1 if is_active else 0,
                    0,
                    password_hash,
                    "2026-03-22T10:00:00+00:00",
                    "",
                    "",
                ),
            )

    def test_login_success_and_me(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        response = self.client.post("/auth/login", json={"username": "admin", "password": "StrongPass123A!"})
        self.assertEqual(response.status_code, 200)
        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        payload = me_response.json()
        self.assertTrue(payload["authenticated"])
        self.assertEqual(payload["user_id"], payload["user"]["user_id"])
        self.assertEqual(payload["username"], "admin")
        self.assertEqual(payload["display_name"], "Admin")
        self.assertEqual(payload["role"], "admin")
        self.assertIsInstance(payload["capabilities"], list)
        self.assertEqual(payload["user"]["username"], "admin")
        self._header_fallback_patch.start()

    def test_auth_me_returns_canonical_role_and_capabilities_for_legacy_user_with_blank_role_name(self) -> None:
        self._header_fallback_patch.stop()
        password_hash = self.auth_service._passwords.hash_password("StrongPass123A!")
        self._insert_legacy_user(
            user_id="legacy-admin-1",
            username="legacy-admin",
            display_name="Legacy Admin",
            role="admin",
            password_hash=password_hash,
        )
        self._repatch_auth_service()

        login_response = self.client.post(
            "/auth/login",
            json={"username": "legacy-admin", "password": "StrongPass123A!"},
        )
        self.assertEqual(login_response.status_code, 200)

        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        payload = me_response.json()
        self.assertTrue(payload["authenticated"])
        self.assertEqual(payload["role"], "admin")
        self.assertIn("auth.manage", payload["capabilities"])
        self.assertEqual(payload["user"]["role_name"], "admin")

        admin_response = self.client.get("/admin/users")
        self.assertEqual(admin_response.status_code, 200)
        self.assertIn("users", admin_response.json())
        self._header_fallback_patch.start()

    def test_auth_me_returns_admin_role_and_capabilities_for_bootstrap_user_with_blank_roles(self) -> None:
        self._header_fallback_patch.stop()
        password_hash = self.auth_service._passwords.hash_password("BootstrapPass123A!")
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap-admin")
        try:
            self._insert_legacy_user(
                user_id="bootstrap-admin-1",
                username="bootstrap-admin",
                display_name="Bootstrap Admin",
                role="",
                password_hash=password_hash,
            )
            self._repatch_auth_service()

            login_response = self.client.post(
                "/auth/login",
                json={"username": "bootstrap-admin", "password": "BootstrapPass123A!"},
            )
            self.assertEqual(login_response.status_code, 200)

            me_response = self.client.get("/auth/me")
            self.assertEqual(me_response.status_code, 200)
            payload = me_response.json()
            self.assertTrue(payload["authenticated"])
            self.assertEqual(payload["role"], "admin")
            self.assertIn("auth.manage", payload["capabilities"])
            self.assertEqual(payload["user"]["role_name"], "admin")
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            self._header_fallback_patch.start()

    def test_login_denies_inactive_user(self) -> None:
        self._header_fallback_patch.stop()
        self.auth_service.create_user(
            username="inactive",
            display_name="Inactive",
            email="inactive@example.com",
            role_name="admin",
            is_active=False,
            must_change_password=False,
            generate_password=False,
            password="StrongPass123A!",
        )
        response = self.client.post("/auth/login", json={"username": "inactive", "password": "StrongPass123A!"})
        self.assertEqual(response.status_code, 401)
        self._header_fallback_patch.start()

    def test_dev_login_allows_username_only_when_enabled(self) -> None:
        self._header_fallback_patch.stop()
        runtime = web_app.settings.runtime
        original_allow_dev_login = runtime.allow_dev_login
        object.__setattr__(runtime, "allow_dev_login", True)
        try:
            self._create_admin_user()
            response = self.client.post("/auth/login", json={"username": "admin", "password": ""})
            self.assertEqual(response.status_code, 200)
            payload = response.json()
            self.assertTrue(payload["authenticated"])
            self.assertTrue(payload["dev_fallback"])
            self.assertEqual(payload["user"]["username"], "admin")
        finally:
            object.__setattr__(runtime, "allow_dev_login", original_allow_dev_login)
            self._header_fallback_patch.start()

    def test_logout_clears_session(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post("/auth/logout")
        self.assertEqual(response.status_code, 200)
        me_response = self.client.get("/auth/me")
        self.assertEqual(me_response.status_code, 200)
        self.assertFalse(me_response.json()["authenticated"])
        self._header_fallback_patch.start()

    def test_auth_me_returns_unauthenticated_shape_without_session(self) -> None:
        self._header_fallback_patch.stop()

        response = self.client.get("/auth/me")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["authenticated"])
        self.assertEqual(payload["user_id"], "")
        self.assertEqual(payload["username"], "")
        self.assertEqual(payload["display_name"], "")
        self.assertEqual(payload["role"], "")
        self.assertEqual(payload["capabilities"], [])
        self._header_fallback_patch.start()

    def test_change_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/auth/change-password",
            json={"current_password": "StrongPass123A!", "new_password": "NewStrongPass123A!"},
        )
        self.assertEqual(response.status_code, 200)
        self.client.post("/auth/logout")
        relogin = self.client.post("/auth/login", json={"username": "admin", "password": "NewStrongPass123A!"})
        self.assertEqual(relogin.status_code, 200)
        self._header_fallback_patch.start()

    def test_bootstrap_admin_creation(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "BootstrapPass123A!")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            service.bootstrap_admin_if_needed()
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)
        user = service.get_user_by_username("bootstrap")
        self.assertIsNotNone(user)
        self.assertEqual(user.role_name, "admin")
        self.assertIn("auth.manage", service.capability_summary("admin"))

    def test_bootstrap_admin_is_idempotent_and_warns_when_no_admin_exists(self) -> None:
        service = AuthService(db_service=self.db_service)
        runtime = web_app.settings.runtime
        original_username = runtime.bootstrap_admin_username
        original_password = runtime.bootstrap_admin_password
        original_display_name = runtime.bootstrap_admin_display_name
        object.__setattr__(runtime, "bootstrap_admin_username", "bootstrap")
        object.__setattr__(runtime, "bootstrap_admin_password", "BootstrapPass123A!")
        object.__setattr__(runtime, "bootstrap_admin_display_name", "Bootstrap Admin")
        try:
            service.bootstrap_admin_if_needed()
            service.bootstrap_admin_if_needed()
            self.assertEqual(len(service.list_users()), 1)

            bootstrap_user = service.get_user_by_username("bootstrap")
            self.assertIsNotNone(bootstrap_user)
            service.update_user(
                bootstrap_user.user_id,
                display_name=bootstrap_user.display_name,
                email=bootstrap_user.email,
                role_name="developer",
                is_active=True,
                must_change_password=bootstrap_user.must_change_password,
            )

            warned_service = AuthService(db_service=self.db_service)
            with warnings.catch_warnings(record=True) as caught:
                warnings.simplefilter("always")
                warned_service.bootstrap_admin_if_needed()
            self.assertTrue(any("no admin account" in str(item.message).lower() for item in caught))
        finally:
            object.__setattr__(runtime, "bootstrap_admin_username", original_username)
            object.__setattr__(runtime, "bootstrap_admin_password", original_password)
            object.__setattr__(runtime, "bootstrap_admin_display_name", original_display_name)

    def test_create_user_with_generated_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/admin/users",
            json={
                "username": "dev1",
                "display_name": "Developer One",
                "email": "dev1@example.com",
                "role_name": "developer",
                "is_active": True,
                "must_change_password": True,
                "generate_password": True,
                "password": "",
            },
        )
        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["generated_password"])
        self.assertEqual(payload["user"]["username"], "dev1")
        self._header_fallback_patch.start()

    def test_create_user_with_manual_password(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        response = self.client.post(
            "/admin/users",
            json={
                "username": "dev2",
                "display_name": "Developer Two",
                "email": "dev2@example.com",
                "role_name": "developer",
                "is_active": True,
                "must_change_password": True,
                "generate_password": False,
                "password": "ManualPass123A!",
            },
        )
        self.assertEqual(response.status_code, 200)
        self.assertNotIn("generated_password", response.json())
        self.assertIsNotNone(self.auth_service.get_user_by_username("dev2"))
        self._header_fallback_patch.start()

    def test_password_reset_flow(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        created = self.auth_service.create_user(
            username="dev-reset",
            display_name="Dev Reset",
            email="dev-reset@example.com",
            role_name="developer",
            is_active=True,
            must_change_password=False,
            generate_password=False,
            password="OriginalPass123A!",
        )
        self._login()
        response = self.client.post(
            f"/admin/users/{created.user.user_id}/reset-password",
            json={"generate_password": True, "must_change_password": True},
        )
        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["generated_password"])
        self._header_fallback_patch.start()

    def test_admin_page_access_control_and_unauthorized_admin_endpoints(self) -> None:
        self._header_fallback_patch.stop()
        response = self.client.get("/admin/users")
        self.assertEqual(response.status_code, 401)
        self._header_fallback_patch.start()

    def test_role_capability_editing_and_policy_editing(self) -> None:
        self._header_fallback_patch.stop()
        self._create_admin_user()
        self._login()
        role_response = self.client.put("/admin/roles/developer", json={"description": "Developer role"})
        self.assertEqual(role_response.status_code, 200)
        caps_response = self.client.put(
            "/admin/roles/developer/capabilities",
            json={"capabilities": ["task.read", "run.read_own"]},
        )
        self.assertEqual(caps_response.status_code, 200)
        self.assertEqual(caps_response.json()["capabilities"], ["run.read_own", "task.read"])
        policy_response = self.client.put(
            "/admin/policies/developer",
            json={
                "repo_allowlist": ["sample"],
                "repo_denylist": [],
                "jira_project_allowlist": ["TEL"],
                "jira_project_denylist": [],
                "protected_branch_prefixes": ["main"],
                "dry_run_only": True,
                "publication_requires_pr": True,
            },
        )
        self.assertEqual(policy_response.status_code, 200)
        self.assertEqual(policy_response.json()["policy"]["repo_allowlist"], ["sample"])
        self._header_fallback_patch.start()

    def test_list_repos_endpoint_returns_registered_repos(self) -> None:
        repos = [
            RepoMetadata(
                repo_id="sample",
                root_path="/repos/sample",
                local_path="/repos/sample",
                remote_url="https://bitbucket.org/acme/sample-repo.git",
                display_name="Sample Repo",
                default_branch="main",
                indexed_at="2026-03-20T00:00:00+00:00",
                status="registered",
                sync_status="up_to_date",
                last_sync_at="2026-03-20T00:10:00+00:00",
            )
        ]

        with patch("web_app.RepoOnboardingService") as mocked_service_class, patch.object(
            web_app._repo_index_service,
            "get_repo_profile",
            return_value=RepoProfile(
                repo_id="sample",
                indexed_at="2026-03-20T00:00:00+00:00",
                primary_stack="dotnet",
                detected_stacks=["dotnet"],
                source_roots=["src"],
                test_roots=["tests"],
                framework_markers=["aspnet-core", "mediatr"],
                solution_files=["Catalog.sln"],
                project_files=["src/Catalog.Api/Catalog.Api.csproj"],
                test_projects=["tests/Catalog.Tests/Catalog.Tests.csproj"],
                controller_count=3,
                route_count=12,
                handler_count=6,
                validator_count=4,
            ),
        ), patch.object(
            web_app._repo_index_service,
            "get_glossary",
            return_value=None,
        ):
            mocked_service_class.return_value.list_repos.return_value = repos
            response = self.client.get(
                "/repos",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["repos"][0]["repo_id"], "sample")
        self.assertEqual(payload["repos"][0]["remote_url"], "https://bitbucket.org/acme/sample-repo.git")
        self.assertEqual(payload["repos"][0]["local_path"], "/repos/sample")
        self.assertIn("profile", payload["repos"][0])
        self.assertIn("index_status", payload["repos"][0])
        self.assertIn("indexed_head", payload["repos"][0])
        self.assertIn("current_local_head", payload["repos"][0])
        self.assertIn("sync_status", payload["repos"][0])
        self.assertIn("last_sync_at", payload["repos"][0])
        self.assertEqual(payload["repos"][0]["profile"]["primary_stack"], "dotnet")
        self.assertEqual(payload["repos"][0]["profile"]["controller_count"], 3)
        self.assertEqual(payload["repos"][0]["profile"]["route_count"], 12)

    def test_onboard_repo_endpoint_returns_onboarding_result(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.onboard_repo.return_value = RepoOnboardingResult(
                repo_id="sample",
                remote_url="https://bitbucket.org/acme/sample-repo.git",
                local_path="/repos/sample",
                status="registered",
                message="Repository onboarded successfully.",
            )
            response = self.client.post(
                "/repos/onboard",
                json={
                    "repo_id": "sample",
                    "display_name": "Sample Repo",
                    "remote_url": "https://bitbucket.org/acme/sample-repo.git",
                    "default_branch": "main",
                },
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["local_path"], "/repos/sample")

    def test_onboard_repo_endpoint_blocks_without_permission(self) -> None:
        response = self.client.post(
            "/repos/onboard",
            json={
                "repo_id": "sample",
                "display_name": "Sample Repo",
                "remote_url": "https://bitbucket.org/acme/sample-repo.git",
                "default_branch": "main",
            },
            headers={
                "X-Actor-Id": "dev-1",
                "X-Actor-Role": "developer",
                "X-Source-Channel": "api",
            },
        )

        self.assertEqual(response.status_code, 403)

    def test_reindex_repo_endpoint_returns_status_payload(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.reindex_repo.return_value = {
                "repo_id": "sample",
                "index_status": "ready",
                "indexed_head": "abc123",
                "indexed_at": "2026-03-22T10:00:00+00:00",
                "current_local_head": "abc123",
                "reindex_required": False,
                "message": "Repository understanding artifacts rebuilt successfully.",
            }
            response = self.client.post(
                "/repos/sample/reindex",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["index_status"], "ready")
        self.assertEqual(
            response.json()["message"],
            "Repository understanding artifacts rebuilt successfully.",
        )

    def test_sync_repo_endpoint_returns_status_payload(self) -> None:
        with patch("web_app.RepoOnboardingService") as mocked_service_class:
            mocked_service_class.return_value.sync_repo.return_value = {
                "repo_id": "sample",
                "sync_status": "synced",
                "current_branch": "main",
                "current_local_head": "abc123",
                "remote_head": "abc123",
                "indexed_head": "abc123",
                "indexed_at": "2026-03-22T10:00:00+00:00",
                "index_status": "ready",
                "reindex_required": False,
                "last_sync_at": "2026-03-22T11:00:00+00:00",
                "message": "Repository sync completed successfully.",
            }
            response = self.client.post(
                "/repos/sample/sync",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["repo_id"], "sample")
        self.assertEqual(response.json()["sync_status"], "synced")

    def test_analyze_task_workflow_endpoint_returns_product_result(self) -> None:
        run = self._create_persisted_run(
            goal="Analyze Jira task",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "title": "TEL-123 Checkout issue",
                    "goal": "Clarify the checkout task.",
                    "context": "Users cannot complete checkout.",
                    "requirements": ["Fix checkout validation flow."],
                    "acceptance_criteria": [],
                    "risks": ["Payment edge cases are unclear."],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Checkout files were found in repository context.",
                "repo_relevance_confidence": 0.88,
                "recommendation": "Clarify acceptance criteria before implementation planning.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-123", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "analyze_task")
        self.assertEqual(payload["result"]["task_quality_summary"], "Task is usable but still needs a few clarifications.")
        self.assertTrue(payload["result"]["missing_details"])
        self.assertTrue(payload["result"]["concrete_questions"])
        self.assertIn("What exact observable behavior should change for the user when this task is complete?", payload["result"]["concrete_questions"])
        self.assertEqual(payload["result"]["technical_run"]["run_id"], run.run_id)

    def test_structure_task_workflow_endpoint_returns_structured_result(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Improve search results",
                    "goal": "Make the search results more relevant.",
                    "context": "Search quality needs improvement for catalog users.",
                    "requirements": ["Tune ranking signals."],
                    "acceptance_criteria": ["Search results should prioritize exact matches."],
                    "risks": ["Ranking changes may affect category pages."],
                },
                "recommendation": "Review the structured task and confirm the acceptance criteria.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "Improve search result relevance for the catalog."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "structure_task")
        self.assertEqual(payload["result"]["title"], "Improve search results")
        self.assertEqual(payload["result"]["acceptance_criteria"][0], "Search results should prioritize exact matches.")
        self.assertEqual(payload["result"]["technical_run"]["run_id"], run.run_id)

    def test_structure_task_workflow_falls_back_to_acceptance_criteria(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Improve search results",
                    "goal": "Make the search results more relevant.",
                    "context": "Search quality needs improvement for catalog users.",
                    "requirements": ["Tune ranking signals."],
                    "acceptance_criteria": [],
                    "risks": [],
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "Improve search result relevance for the catalog."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["result"]["acceptance_criteria"])

    def test_structure_task_workflow_keeps_role_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update services.permission_service.RoleMapper",
                    "goal": "Modify src/security/roles.py for the new role.",
                    "context": "Use admin.service.sync to clone permissions from developer.",
                    "requirements": ["Change services.permission_service.sync_role()."],
                    "acceptance_criteria": [],
                    "risks": ["src/security/roles.py may diverge from admin.service.sync."],
                },
                "recommendation": "Review the proposed files before implementation.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "реалізуй нову роль і дай такі самі права як у dev"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(
            sorted(result.keys()),
            sorted(["title", "summary", "description", "acceptance_criteria", "risks", "open_questions", "recommendation", "technical_run"]),
        )
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        self.assertIn("Уточніть назву", result["recommendation"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("src/", serialized)
        self.assertNotIn(".py", serialized)
        self.assertNotIn("permission_service", serialized)
        self.assertNotIn("service.sync", serialized)

    def test_structure_task_workflow_keeps_report_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update reports/manufacturer.py",
                    "goal": "Modify reports.manufacturer.builder for report export.",
                    "context": "Use services.report_export.add_manufacturer_field in src/reports/export.py.",
                    "requirements": ["Update reports.manufacturer.builder()."],
                    "acceptance_criteria": [],
                    "risks": ["reports/export.py may need synchronized changes."],
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "додай поле manufacturer у звіт"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("reports/", serialized)
        self.assertNotIn(".py", serialized)
        self.assertNotIn("builder", serialized.lower())

    def test_structure_task_workflow_keeps_sms_request_repo_blind(self) -> None:
        run = self._create_persisted_run(
            goal="Structure free text",
            mode="spec",
            detail_payload={
                "spec_result": {
                    "title": "Update sms.service.template_handler",
                    "goal": "Modify src/notifications/sms.py template flow.",
                    "context": "Use notifications.sms.TemplateService.update_template().",
                    "requirements": ["Change sms.template_handler()."],
                    "acceptance_criteria": [],
                    "risks": ["src/notifications/sms.py may affect delivery flow."],
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/structure-task",
                json={"free_text": "оновити шаблон смс"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "uk",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertTrue(result["acceptance_criteria"])
        self.assertTrue(result["open_questions"])
        serialized = json.dumps(result, ensure_ascii=False)
        self.assertNotIn("src/", serialized)
        self.assertNotIn(".py", serialized)
        self.assertNotIn("templateservice", serialized.lower())

    def test_implementation_plan_workflow_short_circuits_repo_mismatch(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            status="repo_mismatch",
            detail_payload={
                "repo_relevance_status": "repo_mismatch",
                "repo_relevance_reason": "The task points to the billing service, not this repo.",
                "repo_relevance_next_action": "Select the billing repository.",
                "recommendation": "Select the billing repository.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-456", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "implementation_plan")
        self.assertEqual(payload["result"]["repo_match"], "mismatch")
        self.assertEqual(payload["result"]["recommendation"], "Select the billing repository.")
        self.assertEqual(payload["result"]["change_actions"], [])
        self.assertEqual(payload["result"]["likely_files"], ["Could not determine affected files"])

    def test_implementation_plan_workflow_returns_actionable_file_level_plan(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "goal": "Fix checkout validation failures.",
                    "requirements": [
                        "Modify the checkout validator to reject empty delivery addresses.",
                        "Add a guard for missing payment method selection.",
                    ],
                    "risks": ["Checkout validation may change existing edge-case behavior."],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Checkout validation files and symbols were found.",
                "repo_relevance_confidence": 0.91,
                "repo_context_summary": {
                    "resolved_target_files": ["src/checkout/validator.py", "tests/test_checkout_validator.py"],
                    "resolved_symbols": {"checkout.validate_order": ["src/checkout/validator.py"]},
                },
                "validation_result": {
                    "validation_profile_used": "pytest tests/test_checkout_validator.py",
                    "targeted_validation": True,
                },
                "recommendation": "Update the validator first, then run the targeted checkout tests.",
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-456", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        result = payload["result"]
        self.assertIn("src/checkout/validator.py", result["likely_files"])
        self.assertTrue(result["likely_file_details"])
        self.assertEqual(result["likely_file_details"][0]["name"], "src/checkout/validator.py")
        self.assertGreater(result["likely_file_details"][0]["confidence"], 0.5)
        self.assertTrue(result["likely_file_details"][0]["reason"])
        self.assertTrue(result["likely_module_details"])
        self.assertTrue(result["change_actions"])
        self.assertEqual(result["change_actions"][0]["file"], "src/checkout/validator.py")
        self.assertIn(result["change_actions"][0]["action"], {"add", "modify", "delete"})
        serialized = json.dumps(result).lower()
        self.assertNotIn("relevant files", serialized)
        self.assertNotIn("appropriate modules", serialized)
        self.assertNotIn("related code", serialized)

    def test_implementation_plan_workflow_returns_fallback_areas_when_confidence_is_low(self) -> None:
        run = self._create_persisted_run(
            goal="Implementation plan",
            mode="spec",
            repo_id="sample",
            detail_payload={
                "spec_result": {
                    "goal": "Adjust reporting filters.",
                    "requirements": [],
                    "risks": [],
                },
                "repo_relevance_status": "relevant",
                "repo_relevance_reason": "Context produced weak but plausible subsystem matches.",
                "repo_relevance_confidence": 0.24,
                "repo_context_summary": {
                    "files_used": ["src/reports/filters.py", "src/reports/query_builder.py"],
                    "file_selection": {
                        "src/reports/filters.py": ["path keyword target: reports", "content match"],
                        "src/reports/query_builder.py": ["path keyword target: query", "content match"],
                    },
                },
            },
        )

        with patch("web_app._execute_tracked_api_run", return_value=run):
            response = self.client.post(
                "/workflows/implementation-plan",
                json={"jira_ticket": "TEL-457", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["likely_files"], ["Could not determine affected files"])
        self.assertTrue(result["closest_areas"])
        self.assertEqual(result["closest_areas"][0]["area"], "src/reports")
        self.assertIn("No exact file match found", result["recommendation"])
        self.assertTrue(result["change_actions"])
        self.assertEqual(result["change_actions"][0]["file"], "Could not determine affected files")

    def test_pre_review_workflow_returns_verdict_and_drill_down(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "approved",
                    "summary": "Changes are ready for human review.",
                    "issues": [],
                    "approved_files": ["src/app.py", "tests/test_app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 2,
                        "file_paths": ["src/app.py", "tests/test_app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "src/app.py", "change_type": "modified"},
                        {"file_path": "tests/test_app.py", "change_type": "modified"},
                    ],
                },
                "recommendation": "Proceed to human review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-789", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "pre_review")
        self.assertEqual(payload["result"]["verdict"], "ready_for_review")
        self.assertEqual(payload["result"]["review_verdict"], "ready_for_review")
        self.assertEqual(payload["result"]["decision_statement"], "Safe to proceed to review")
        self.assertGreater(payload["result"]["review_verdict_confidence"], 0.7)
        self.assertTrue(payload["result"]["ready_for_crucible"])
        self.assertEqual(payload["result"]["blocking_explanation"], "No blocking issues were found in the focused review context. The change is safe to proceed to review.")
        technical_run = payload["result"]["technical_run"]
        self.assertEqual(technical_run["run_id"], run.run_id)
        with patch("web_app.root_agent.RunService", return_value=RunService(storage_dir=self.storage_dir, persist=True)):
            detail_response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )
        self.assertEqual(detail_response.status_code, 200)

    def test_pre_review_workflow_returns_actionable_required_fixes(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["src/app.py still returns the legacy payload shape."],
                    "approved_files": ["src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified"}],
                },
                "recommendation": "Fix the payload shape before asking for review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-789", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["result"]["verdict"], "blocked_actionable")
        self.assertEqual(payload["result"]["review_verdict"], "blocked_actionable")
        self.assertEqual(payload["result"]["decision_statement"], "Do NOT open review yet")
        self.assertGreater(payload["result"]["review_verdict_confidence"], 0.8)
        self.assertIn("Blocked because", payload["result"]["blocking_explanation"])
        self.assertTrue(payload["result"]["issue_details"])
        self.assertEqual(payload["result"]["issue_details"][0]["file"], "src/app.py")
        self.assertEqual(payload["result"]["issue_details"][0]["severity"], "critical")
        self.assertEqual(payload["result"]["issue_details"][0]["impact"], "breaks API")
        self.assertEqual(payload["result"]["issue_details"][0]["evidence_type"], "review")
        self.assertTrue(payload["result"]["issue_details"][0]["evidence_source"])
        self.assertTrue(payload["result"]["issue_details"][0]["evidence_snippet"])
        self.assertTrue(payload["result"]["required_fixes"])
        self.assertEqual(payload["result"]["required_fixes"][0]["file"], "src/app.py")
        self.assertIn("legacy payload shape", payload["result"]["required_fixes"][0]["what_to_fix"])
        self.assertIn("Update src/app.py", payload["result"]["required_fixes"][0]["exact_action"])

    def test_pre_review_workflow_returns_mixed_severity_issue_details(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked by multiple issues.",
                    "issues": [
                        "src/api/orders.py changes the API payload shape.",
                        "tests/test_orders.py is missing validation coverage for the new branch.",
                        "src/orders/service.py changes behavior for the legacy fallback path.",
                    ],
                    "approved_files": ["src/api/orders.py", "tests/test_orders.py", "src/orders/service.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 3,
                        "file_paths": ["src/api/orders.py", "tests/test_orders.py", "src/orders/service.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "src/api/orders.py", "change_type": "modified"},
                        {"file_path": "tests/test_orders.py", "change_type": "modified"},
                        {"file_path": "src/orders/service.py", "change_type": "modified"},
                    ],
                },
                "recommendation": "Fix the blocking issues before review.",
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-790", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_actionable")
        self.assertEqual([item["severity"] for item in result["issue_details"]], ["critical", "critical", "warning"])
        self.assertEqual(
            [item["impact"] for item in result["issue_details"]],
            ["breaks API", "missing validation", "risk of regression"],
        )

    def test_pre_review_workflow_marks_fix_and_retry_unavailable_when_not_actionable(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked.",
                    "issues": ["runtime_error still needs investigation."],
                    "approved_files": [],
                },
                "diff_result": {"diff_available": False, "files": []},
                "implementation_result": {"artifact_summary": {"files_count": 0}},
                "repo_context_summary": {"resolved_target_files": [], "files_used": []},
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-791", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertFalse(result["fix_and_retry_actionable"])
        self.assertTrue(result["fix_and_retry_block_reason"])

    def test_pre_review_attaches_dockerfile_issue_only_to_dockerfile(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review is blocked by a Dockerfile issue.",
                    "issues": ["Dockerfile installs runtime dependencies in the final image stage."],
                    "approved_files": ["Dockerfile", "src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 2,
                        "file_paths": ["Dockerfile", "src/app.py"],
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [
                        {"file_path": "Dockerfile", "change_type": "modified"},
                        {"file_path": "src/app.py", "change_type": "modified"},
                    ],
                },
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-792", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_actionable")
        self.assertEqual(len(result["issue_details"]), 1)
        self.assertEqual(result["issue_details"][0]["file"], "Dockerfile")
        self.assertEqual(result["required_fixes"][0]["file"], "Dockerfile")
        self.assertNotIn("src/app.py", [item["file"] for item in result["issue_details"]])

    def test_pre_review_returns_blocked_insufficient_artifact_without_file_level_issues(self) -> None:
        run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Review cannot be completed yet.",
                    "issues": ["A deployment concern still needs review."],
                    "approved_files": [],
                },
                "diff_result": {"diff_available": False, "files": []},
                "implementation_result": {"artifact_summary": {"files_count": 0, "file_paths": []}},
            },
        )

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(run, self._load_persisted_run_detail(run))), patch(
            "web_app._execute_tracked_api_run",
            return_value=run,
        ):
            response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-793", "repo_id": "sample"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        result = response.json()["result"]
        self.assertEqual(result["review_verdict"], "blocked_insufficient_artifact")
        self.assertFalse(result["issue_details"])
        self.assertFalse(result["required_fixes"])
        self.assertFalse(result["files_to_check"])
        self.assertTrue(result["global_blockers"])

    def test_fix_and_retry_workflow_retries_blocked_review_with_focused_fixes(self) -> None:
        source_run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["src/app.py still returns the legacy payload shape."],
                    "approved_files": ["src/app.py"],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 1,
                    }
                },
                "diff_result": {
                    "diff_available": True,
                    "files": [{"file_path": "src/app.py", "change_type": "modified"}],
                },
                "repo_context_summary": {
                    "resolved_target_files": ["src/app.py"],
                    "files_used": ["src/app.py"],
                },
                "final_result_summary": "Changes are not ready for human review yet.",
                "root_cause_summary": "src/app.py still returns the legacy payload shape.",
            },
        )
        child_run = self._create_persisted_run(
            goal="Implement fix",
            mode="implement",
            repo_id="sample",
            detail_payload={
                "implementation_result": {
                    "final_status": "dry_run_complete",
                },
            },
        )
        source_detail = RunService(storage_dir=self.storage_dir, persist=True).load_run_detail(
            source_run.run_id,
            run_record=source_run,
            log_path=source_run.log_path,
        )
        captured_payload: dict[str, object] = {}

        def _fake_retry(command, actor_context, *, action_payload=None):
            _ = (command, actor_context)
            captured_payload.update(dict(action_payload or {}))
            return AgentResult(
                agent_name="runs",
                output_text="Focused retry started.",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "artifact_type": "run_retry",
                    "action": "retry",
                    "source_run": source_run,
                    "run_record": source_run,
                    "new_run_record": child_run,
                    "message": "Focused retry started.",
                    "blocked_reason": "",
                    "success": True,
                },
            )

        with patch("web_app._load_visible_run", return_value=source_run), patch(
            "web_app._load_run_detail_for_record",
            return_value=source_detail,
        ), patch("web_app._run_action_command", side_effect=_fake_retry):
            response = self.client.post(
                "/workflows/fix-and-retry",
                json={"run_id": source_run.run_id, "note": "Keep the patch small."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["workflow"], "fix_and_retry")
        self.assertEqual(payload["new_run_id"], child_run.run_id)
        self.assertTrue(payload["success"])
        self.assertEqual(captured_payload["workflow_name"], "fix_and_retry")
        self.assertIn("STRICT FIX MODE:", str(captured_payload["refinement_prompt"]))
        self.assertIn("Only fix the listed review-blocking issues.", str(captured_payload["refinement_prompt"]))
        self.assertIn("src/app.py", str(captured_payload["refinement_prompt"]))
        self.assertIn("legacy payload shape", str(captured_payload["refinement_prompt"]))
        self.assertIn("Keep the patch small.", str(captured_payload["refinement_prompt"]))
        self.assertEqual(captured_payload["fix_files"], ["src/app.py"])
        self.assertIn("src/app.py", str(captured_payload["repo_query_input"]))

    def test_fix_and_retry_workflow_blocks_when_no_real_files_or_artifacts_exist(self) -> None:
        source_run = self._create_persisted_run(
            goal="Pre review",
            mode="review",
            repo_id="sample",
            detail_payload={
                "review_result": {
                    "status": "blocked",
                    "summary": "Changes are not ready for human review yet.",
                    "issues": ["runtime_error still needs investigation."],
                    "approved_files": [],
                },
                "diff_result": {
                    "diff_available": False,
                    "files": [],
                },
                "implementation_result": {
                    "artifact_summary": {
                        "files_count": 0,
                    }
                },
                "repo_context_summary": {
                    "resolved_target_files": [],
                    "files_used": [],
                },
                "final_result_summary": "Changes are not ready for human review yet.",
                "root_cause_summary": "runtime_error still needs investigation.",
            },
        )
        source_detail = RunService(storage_dir=self.storage_dir, persist=True).load_run_detail(
            source_run.run_id,
            run_record=source_run,
            log_path=source_run.log_path,
        )

        with patch("web_app._load_visible_run", return_value=source_run), patch(
            "web_app._load_run_detail_for_record",
            return_value=source_detail,
        ), patch("web_app._run_action_command") as mocked_retry:
            response = self.client.post(
                "/workflows/fix-and-retry",
                json={"run_id": source_run.run_id, "note": "Keep the patch small."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["success"])
        self.assertEqual(payload["status"], "retry_not_actionable")
        self.assertTrue("артефакт" in payload["message"].lower() or "concrete" in payload["message"].lower())
        mocked_retry.assert_not_called()

    def test_workflow_endpoints_map_to_expected_run_modes(self) -> None:
        spec_run = self._create_persisted_run(goal="Spec workflow", mode="spec")
        review_run = self._create_persisted_run(goal="Review workflow", mode="review")
        captured_modes: list[str] = []

        def _fake_execute(*, request_body, actor_context, workflow_name=""):
            _ = actor_context
            _ = workflow_name
            captured_modes.append(request_body.mode)
            return spec_run if request_body.mode == "spec" else review_run

        with patch("web_app._find_latest_implementation_run_with_artifact", return_value=(review_run, self._load_persisted_run_detail(review_run))), patch(
            "web_app._execute_tracked_api_run",
            side_effect=_fake_execute,
        ):
            analyze_response = self.client.post(
                "/workflows/analyze-task",
                json={"jira_ticket": "TEL-1", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
            )
            pre_review_response = self.client.post(
                "/workflows/pre-review",
                json={"jira_ticket": "TEL-2", "repo_id": "sample"},
                headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api"},
            )

        self.assertEqual(analyze_response.status_code, 200)
        self.assertEqual(pre_review_response.status_code, 200)
        self.assertEqual(captured_modes, ["spec", "review"])

    def test_list_runs_returns_own_runs(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        own_actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        other_actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        own_run = service.start_run("Own run", repo_id="sample", actor_context=own_actor)
        service.finish_run(own_run.run_id, "success")
        other_run = service.start_run("Other run", repo_id="sample", actor_context=other_actor)
        service.finish_run(other_run.run_id, "success")

        with patch("web_app.RunService", return_value=service):
            response = self.client.get(
                "/runs",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                    "X-Display-Name": "Developer One",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["runs"][0]["run_id"], own_run.run_id)
        self.assertEqual(payload["runs"][0]["actor_display_name"], "Developer One")
        self.assertIn("actor_username", payload["runs"][0])
        self.assertTrue(payload["runs"][0]["started_at"])

    def test_list_runs_denies_broad_access_without_run_read_all(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        run = service.start_run("Own run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.RunService", return_value=service):
            response = self.client.get(
                "/runs?actor_id=lead-1",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["permission_decision"]["capability"], "run.read_all")

    def test_list_runs_uses_lightweight_projection_without_run_detail_hydration(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="dev-1",
            actor_type="user",
            role="developer",
            source_channel="api",
            display_name="Developer One",
        )
        run = service.start_run("Fast list run", repo_id="sample", actor_context=actor)
        finished = service.finish_run(run.run_id, "success")
        service.persist_run_detail(
            run.run_id,
            {"mode": "implement", "publication_result": {"branch_name": "feature/ai/fast-list"}},
            log_path=finished.log_path,
        )

        with patch("web_app.RunService", return_value=service), patch.object(
            service,
            "load_run_detail",
            side_effect=AssertionError("GET /runs should not hydrate run detail rows"),
        ):
            response = self.client.get(
                "/runs",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["count"], 1)
        self.assertEqual(payload["runs"][0]["run_id"], run.run_id)
        self.assertEqual(payload["runs"][0]["branch_name"], "")

    def test_show_run_detail(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Detail run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["run"]["run_id"], run.run_id)
        self.assertEqual(response.json()["run"]["mode"], "unknown")

    def test_show_run_detail_hydrates_implementation_sections(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Implementation run", repo_id="sample", actor_context=actor)
        service.attach_scm(
            run.run_id,
            {
                "branch_name": "feature/ai/implementation-run",
                "commit_hash": "abc123",
                "remote_url": "https://bitbucket.org/acme/sample-repo.git",
            },
        )
        service.attach_publication(
            run.run_id,
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-1",
        )
        finished_run = service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            {
                "files": [
                    {
                        "relative_path": "src/app.py",
                        "status": "modified",
                        "diff": "--- a/src/app.py\n+++ b/src/app.py",
                    }
                ]
            },
        )
        service.persist_review_comments(
            run.run_id,
            [{"file_path": "src/app.py", "severity": "warning", "title": "Check"}],
        )
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 2 test(s) failed",
                "implementation_result": {
                    "final_status": "applied",
                    "sync_status": "synced",
                    "local_head_before": "local-old",
                    "remote_head": "remote-new",
                    "synced_before_run": True,
                    "repo_relevance_status": "relevant",
                    "repo_relevance_confidence": 0.82,
                    "repo_relevance_reason": "Repo context produced plausible file or symbol matches for the request.",
                    "repo_relevance_next_action": "Continue with the repo-aware run.",
                    "validation_outcome_type": "validation_failed_code",
                    "change_summary": "Prepared 1 file change(s): src/app.py",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Implementation run",
                        "file_count": 1,
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                        "files_changed": 1,
                        "files_created": 0,
                        "files_deleted": 0,
                    },
                    "dry_run_apply_result": {
                        "repo_id": "sample",
                        "root_path": self.workspace_root.as_posix(),
                        "dry_run": True,
                        "applied_files": [],
                        "skipped_files": [],
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "validation_failed",
                    },
                },
                "publication_result": {"publication_status": "success", "review_status": "success"},
                "validation_result": {
                    "overall_status": "failed",
                    "passed": False,
                    "outcome_type": "validation_failed_code",
                    "validation_scope": "changed_files",
                    "validation_profile_used": "python_targeted",
                    "targeted_validation": True,
                    "environment_related_failure": False,
                    "environment_prepared": True,
                    "dependency_install_status": "prepared",
                    "environment_setup_logs": "Dependency source detected: requirements.txt",
                    "total_tests": 2,
                    "passed_tests": 0,
                    "failed_tests": 2,
                    "failed_test_cases": [{"name": "tests/test_app.py::test_run", "error_type": "AssertionError", "message": "boom"}],
                    "stdout": "stdout",
                    "stderr": "stderr",
                },
                "diff_result": {"files": [], "reason": "validation_failed_before_apply"},
            },
            log_path=finished_run.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["mode"], "implement")
        self.assertTrue(payload["root_cause_summary"])
        self.assertEqual(payload["sync_status"], "synced")
        self.assertTrue(payload["synced_before_run"])
        self.assertEqual(payload["repo_relevance_status"], "relevant")
        self.assertEqual(payload["publication_result"]["branch_name"], "feature/ai/implementation-run")
        self.assertFalse(payload["diff_result"]["diff_available"])
        self.assertEqual(payload["diff_result"]["reason"], "validation_failed_before_apply")
        self.assertEqual(payload["validation_result"]["failed_tests"], 2)
        self.assertEqual(payload["validation_result"]["outcome_type"], "validation_failed_code")
        self.assertTrue(payload["validation_result"]["targeted_validation"])
        self.assertTrue(payload["validation_result"]["environment_prepared"])
        self.assertEqual(payload["validation_result"]["dependency_install_status"], "prepared")
        self.assertEqual(payload["implementation_result"]["dry_run_apply_result"]["skip_reason"], "validation_failed")
        self.assertEqual(payload["review_comments"][0]["file_path"], "src/app.py")
        self.assertEqual(payload["run_outcome_type"], "failed_code")
        self.assertTrue(payload["final_result_summary"])
        self.assertTrue(payload["recommendation"])

    def test_show_run_detail_hydrates_spec_review_and_research_sections(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        spec_run = service.start_run("Spec run", repo_id="sample", actor_context=actor)
        spec_finished = service.finish_run(spec_run.run_id, "success")
        service.persist_run_detail(
            spec_run.run_id,
            {
                "mode": "spec",
                "spec_result": {"title": "Spec Title", "goal": "Spec Goal"},
                "repo_context_summary": {"files_used": ["src/app.py"], "chunk_count": 1},
            },
            log_path=spec_finished.log_path,
        )
        review_run = service.start_run("Review run", repo_id="sample", actor_context=actor)
        review_finished = service.finish_run(review_run.run_id, "success")
        service.persist_run_detail(
            review_run.run_id,
            {
                "mode": "review",
                "review_result": {"summary": "Review Summary", "issues": ["Issue A"]},
            },
            log_path=review_finished.log_path,
        )
        research_run = service.start_run("Research run", actor_context=actor)
        research_finished = service.finish_run(research_run.run_id, "success")
        service.persist_run_detail(
            research_run.run_id,
            {
                "mode": "research",
                "research_result": {"answer": "Research answer", "confidence": "high"},
            },
            log_path=research_finished.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            spec_response = self.client.get(f"/runs/{spec_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})
            review_response = self.client.get(f"/runs/{review_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})
            research_response = self.client.get(f"/runs/{research_run.run_id}", headers={"X-Actor-Id": "lead-1", "X-Actor-Role": "techlead", "X-Source-Channel": "api", "X-Lang": "en"})

        self.assertEqual(spec_response.json()["run"]["spec_result"]["title"], "Spec Title")
        self.assertEqual(review_response.json()["run"]["review_result"]["issues"], ["Issue A"])
        self.assertEqual(research_response.json()["run"]["research_result"]["confidence"], "high")

    def test_show_run_detail_hydrates_no_changes_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No changes run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "No changes generated by agent",
                "implementation_result": {
                    "final_status": "no_changes",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "No changes run",
                        "file_count": 0,
                        "files_count": 0,
                        "file_paths": [],
                        "files_changed": 0,
                        "files_created": 0,
                        "files_deleted": 0,
                        "reason_if_empty": "agent produced no changes",
                    },
                    "dry_run_apply_result": {
                        "repo_id": "sample",
                        "root_path": self.workspace_root.as_posix(),
                        "dry_run": True,
                        "applied_files": [],
                        "skipped_files": [],
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "no_changes",
                    },
                },
                "validation_result": {
                    "overall_status": "skipped",
                    "passed": False,
                    "total_tests": 0,
                    "passed_tests": 0,
                    "failed_tests": 0,
                    "failed_test_cases": [],
                    "stdout": "",
                    "stderr": "",
                    "warnings": ["Skipped because no changes were generated."],
                },
                "diff_result": {"files": [], "reason": "no_changes"},
            },
            log_path=finished_run.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["status"], "no_changes")
        self.assertEqual(payload["implementation_result"]["final_status"], "no_changes")
        self.assertEqual(payload["diff_result"]["reason"], "no_changes")
        self.assertTrue(payload["root_cause_summary"])
        self.assertEqual(payload["run_outcome_type"], "success_no_changes")
        self.assertEqual(payload["final_result_summary"], "No changes generated by agent.")
        self.assertEqual(payload["recommendation"], "Refine the request.")

    def test_show_retry_child_run_detail_keeps_current_no_changes_state_clean(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        parent = service.start_run("Parent failed run", repo_id="sample", actor_context=actor)
        parent = service.finish_run(parent.run_id, "failed")
        service.persist_run_detail(
            parent.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation environment is not ready.",
                "final_result_summary": "Validation failed due to missing dependencies.",
                "recommendation": "Install missing dependencies and retry.",
                "implementation_result": {
                    "final_status": "candidate_validation_failed",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Parent failed run",
                        "file_count": 1,
                        "files_count": 1,
                        "file_paths": ["src/app.py"],
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_environment_not_ready",
                    "environment_related_failure": True,
                    "failed_tests": 0,
                    "errors": ["pytest is not installed"],
                },
            },
            log_path=parent.log_path,
        )
        child = service.start_run(
            "Retry child run",
            repo_id="sample",
            actor_context=actor,
            parent_run_id=parent.run_id,
            retry_note="Retry with smaller scope.",
            retry_context_summary="Validation failed previously because dependencies were missing.",
            retry_context={
                "retry_reason": "validation_failed",
                "instructions": ["Only fix the concrete issue."],
            },
            attempt_index=2,
            total_attempts=3,
        )
        child = service.finish_run(child.run_id, "no_changes")
        service.persist_run_detail(
            child.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "No changes generated by agent",
                "final_result_summary": "No changes generated by agent.",
                "recommendation": "Refine the request.",
                "implementation_result": {
                    "final_status": "no_changes",
                    "artifact_summary": {
                        "artifact_type": "draft_set",
                        "goal": "Retry child run",
                        "file_count": 0,
                        "files_count": 0,
                        "file_paths": [],
                        "reason_if_empty": "agent produced no changes",
                    },
                    "dry_run_apply_result": {
                        "applied": False,
                        "files_written": 0,
                        "files_failed": 0,
                        "skipped": True,
                        "skip_reason": "no_changes",
                    },
                },
                "validation_result": {
                    "overall_status": "failed",
                    "outcome_type": "validation_environment_not_ready",
                    "environment_related_failure": True,
                    "failed_tests": 0,
                    "errors": ["stale inherited validation payload"],
                },
                "diff_result": {"files": [], "reason": "no_changes"},
            },
            log_path=child.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{child.run_id}",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                    "X-Lang": "en",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()["run"]
        self.assertEqual(payload["status"], "no_changes")
        self.assertEqual(payload["validation_result"]["overall_status"], "skipped")
        self.assertEqual(payload["validation_result"]["failed_tests"], 0)
        self.assertFalse(payload["validation_result"]["environment_related_failure"])
        self.assertEqual(payload["implementation_result"]["dry_run_apply_result"]["skip_reason"], "no_changes")
        self.assertTrue(payload["implementation_result"]["dry_run_apply_result"]["skipped"])
        self.assertEqual(payload["diff_result"]["reason"], "no_changes")
        self.assertEqual(payload["previous_attempt_summary"]["run_id"], parent.run_id)

    def test_create_run_endpoint_tracks_non_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        with patch("web_app.RunService", return_value=service), patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="research",
                output_text="Research completed.\nDetails...",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={},
            ),
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "What is repo_context?",
                    "repo_id": "sample",
                    "mode": "research",
                },
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                    "X-Display-Name": "Developer One",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["status"], "success")
        self.assertEqual(payload["mode"], "research")
        self.assertTrue(payload["run_id"])
        self.assertEqual(payload["branch_name"], "")
        self.assertEqual(payload["pr_url"], "")
        self.assertEqual(payload["run"]["steps"][0]["name"], "research")

    def test_create_implementation_run_returns_branch_and_pr_url(self) -> None:
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = RunRecord(
            run_id="implement-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            actor_context=actor,
            scm={"branch_name": "feature/ai/implement-run-1"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
        )

        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="implementation",
                output_text="Implementation completed.",
                success=True,
                task_intent="modify",
                repo_context={},
                metadata={"run_record": run},
            ),
        ) as mocked_run_root_agent, patch(
            "web_app._api_auto_publish_implementation_runs",
            return_value=True,
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "Implement update src/app.py",
                    "repo_id": "sample",
                    "mode": "implement",
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["branch_name"], "feature/ai/implement-run-1")
        self.assertEqual(payload["pr_url"], "https://bitbucket.org/acme/sample-repo/pull-requests/1")
        self.assertEqual(payload["review_url"], "")
        self.assertTrue(mocked_run_root_agent.called)
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["real_apply"])
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_pr"])
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_review"])

    def test_create_implementation_run_requests_review_when_auto_review_is_enabled(self) -> None:
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = RunRecord(
            run_id="implement-run-2",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            actor_context=actor,
            scm={"branch_name": "feature/ai/implement-run-2"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/2",
            review_url="https://crucible.example.invalid/cru/CR-PROJ-2",
        )

        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="implementation",
                output_text="Implementation completed.",
                success=True,
                task_intent="modify",
                repo_context={},
                metadata={"run_record": run},
            ),
        ) as mocked_run_root_agent, patch(
            "web_app._api_auto_publish_implementation_runs",
            return_value=True,
        ), patch(
            "web_app._api_auto_create_review_after_publication",
            return_value=True,
        ):
            response = self.client.post(
                "/runs",
                json={
                    "goal": "Implement update src/app.py",
                    "repo_id": "sample",
                    "mode": "implement",
                },
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["review_url"], "https://crucible.example.invalid/cru/CR-PROJ-2")
        self.assertEqual(response.json()["run"]["review_url"], "https://crucible.example.invalid/cru/CR-PROJ-2")
        self.assertTrue(mocked_run_root_agent.call_args.kwargs["create_review"])

    def test_show_run_steps_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Step run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")
        service.finish_step(run.run_id, "success", "Validation passed.")
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/steps",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["steps"][0]["step_name"], "validation")
        self.assertEqual(response.json()["steps"][0]["error_code"], "")

    def test_show_run_policy_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Policy run", repo_id="sample", actor_context=actor)
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="lead-1",
                actor_role="techlead",
                allowed=False,
                reason="Missing validation path.",
                deny_reason_code="MISSING_VALIDATION",
                scope=PermissionScope(repo_id="sample", source_channel="api"),
                source="fallback",
            ),
        )
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/policy",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["policy_decisions"][0]["deny_reason_code"], "MISSING_VALIDATION")

    def test_show_run_errors_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Error run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")
        service.fail_step(
            run.run_id,
            "Validation did not pass.",
        )
        service.record_policy_decision(
            run.run_id,
            PermissionDecision(
                capability="implementation.apply",
                actor_id="lead-1",
                actor_role="techlead",
                allowed=False,
                reason="Missing validation path.",
                deny_reason_code="MISSING_VALIDATION",
                scope=PermissionScope(repo_id="sample", source_channel="api"),
                source="fallback",
            ),
        )
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/errors",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["failure_summary"]["failure_code"], "MISSING_VALIDATION")
        self.assertEqual(payload["step_errors"][0]["step_name"], "validation")
        self.assertEqual(payload["step_errors"][0]["error_code"], "unexpected")

    def test_show_run_diff_endpoint_returns_existing_diff_artifact(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            DiffResult(
                repo_id="sample",
                root_path=self.workspace_root.as_posix(),
                dry_run=True,
                files=[
                    DiffFile(
                        relative_path="src/app.py",
                        operation_type="update",
                        diff="--- a/src/app.py\n+++ b/src/app.py\n@@\n-return 'old'\n+return 'new'",
                        status="modified",
                    )
                ],
            ),
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["diff_available"])
        self.assertFalse(payload["truncated"])
        self.assertEqual(payload["files"][0]["relative_path"], "src/app.py")
        self.assertEqual(payload["files"][0]["status"], "modified")
        self.assertIn("return 'new'", payload["files"][0]["diff_text"])
        self.assertEqual(payload["total_files_changed"], 1)
        self.assertEqual(payload["total_additions"], 1)
        self.assertEqual(payload["total_deletions"], 1)
        self.assertEqual(payload["files"][0]["change_type"], "modified")
        self.assertTrue(payload["files"][0]["diff_chunks"])

    def test_show_run_diff_endpoint_returns_empty_response_when_no_diff_exists(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["diff_available"])
        self.assertEqual(payload["files"], [])
        self.assertFalse(payload["truncated"])
        self.assertEqual(payload["total_files_changed"], 0)

    def test_show_run_diff_endpoint_marks_truncated_preview(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Truncated diff run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_diff_result(
            run.run_id,
            {
                "repo_id": "sample",
                "root_path": self.workspace_root.as_posix(),
                "dry_run": True,
                "warnings": ["Diff file list truncated to 20 entries."],
                "files": [
                    {
                        "relative_path": "src/huge.py",
                        "operation_type": "update",
                        "diff": "--- a/src/huge.py\n+++ b/src/huge.py\n...[TRUNCATED]",
                        "status": "modified",
                    }
                ],
            },
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/diff",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(response.json()["truncated"])

    def test_show_run_comments_endpoint_returns_existing_comments(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Comments run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")
        service.persist_review_comments(
            run.run_id,
            [
                {
                    "file_path": "src/app.py",
                    "severity": "risk",
                    "title": "Bare exception handler added",
                    "comment": "A bare except can hide unexpected failures.",
                    "suggested_check": "Narrow the exception type.",
                    "line_hint": "12",
                },
                {
                    "file_path": "src/app.py",
                    "severity": "warning",
                    "title": "TODO marker added",
                    "comment": "This diff adds a TODO.",
                    "suggested_check": "Confirm the follow-up is tracked.",
                    "line_hint": "18",
                },
            ],
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/comments",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["comments_available"])
        self.assertEqual(len(payload["comments"]), 2)
        self.assertEqual(payload["comments"][0]["file_path"], "src/app.py")
        self.assertEqual(payload["comments"][0]["severity"], "risk")

    def test_show_run_comments_endpoint_returns_empty_response_when_no_comments_exist(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No comments run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.get(
                f"/runs/{run.run_id}/comments",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["comments_available"])
        self.assertEqual(payload["comments"], [])

    def test_retry_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="api",
            display_name="Admin",
        )
        source_run = service.start_run("Retry run", repo_id="sample", actor_context=actor)
        finished_source = service.finish_run(source_run.run_id, "failed")
        service.persist_run_detail(
            source_run.run_id,
            {
                "mode": "implement",
                "root_cause_summary": "Validation failed: 1 test(s) failed",
            },
            log_path=finished_source.log_path,
        )
        retried_run = RunRecord(
            run_id="retry-run-1",
            goal="Retry run",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            parent_run_id=source_run.run_id,
            repo_id="sample",
            actor_context=actor,
            finished_at="2026-03-20T00:01:00+00:00",
        )
        retried_result = AgentResult(
            agent_name="implementation",
            output_text="retry complete",
            success=True,
            task_intent="modify",
            repo_context={},
            metadata={"run_record": retried_run},
        )

        with patch("web_app.root_agent.RunService", return_value=service), patch(
            "web_app.root_agent.run_implementation_pipeline",
            return_value=retried_result,
        ):
            response = self.client.post(
                f"/runs/{source_run.run_id}/retry",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["source_run_id"], source_run.run_id)
        self.assertEqual(payload["new_run"]["run_id"], "retry-run-1")
        self.assertEqual(payload["new_run"]["parent_run_id"], source_run.run_id)

    def test_cancel_endpoint(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="admin-1",
            actor_type="user",
            role="admin",
            source_channel="api",
            display_name="Admin",
        )
        run = service.start_run("Cancel run", repo_id="sample", actor_context=actor)
        service.start_step(run.run_id, "validation")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/cancel",
                headers={
                    "X-Actor-Id": "admin-1",
                    "X-Actor-Role": "admin",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["run"]["status"], "cancelled")

    def test_review_endpoint_success(self) -> None:
        run = RunRecord(
            run_id="review-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-20T00:00:00+00:00",
            finished_at="2026-03-20T00:01:00+00:00",
            repo_id="sample",
            scm={"branch_name": "feature/ai/review-run-1"},
            pr_url="https://bitbucket.org/acme/sample-repo/pull-requests/1",
            review_url="https://crucible.example.invalid/cru/CR-PROJ-1",
        )
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="review created",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "run_record": run,
                    "review_result": CrucibleReviewResult(
                        success=True,
                        title="AI Review: Implement update src/app.py",
                        repo="sample",
                        branch="feature/ai/review-run-1",
                        reviewers=[],
                        url=run.review_url,
                        review_id="CR-PROJ-1",
                    ),
                    "review_url": run.review_url,
                    "review_status": "created",
                },
            ),
        ):
            response = self.client.post(
                f"/runs/{run.run_id}/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["review_url"], run.review_url)
        self.assertEqual(response.json()["status"], "created")

    def test_review_endpoint_denied_when_permission_is_blocked(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="policy",
                output_text="Access denied.",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={
                    "permission_decision": PermissionDecision(
                        capability="review.create",
                        actor_id="dev-1",
                        actor_role="developer",
                        allowed=False,
                        reason="Review creation is not allowed.",
                        deny_reason_code="ROLE_NOT_ALLOWED",
                        scope=PermissionScope(repo_id="sample", source_channel="api"),
                        source="fallback",
                    ),
                },
            ),
        ):
            response = self.client.post(
                "/runs/review-denied/review",
                headers={
                    "X-Actor-Id": "dev-1",
                    "X-Actor-Role": "developer",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 403)
        self.assertEqual(response.json()["detail"]["permission_decision"]["capability"], "review.create")

    def test_review_endpoint_blocked_when_branch_is_missing(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="branch missing",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={"artifact_type": "run_review"},
            ),
        ):
            response = self.client.post(
                "/runs/review-missing-branch/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)

    def test_review_endpoint_blocked_when_run_is_not_publishable(self) -> None:
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="not publishable",
                success=False,
                task_intent="read",
                repo_context={},
                metadata={"artifact_type": "run_review"},
            ),
        ):
            response = self.client.post(
                "/runs/review-not-publishable/review",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 409)

    def test_approve_endpoint_updates_run_decision(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Approve run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "success")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                json={"note": "Ship it."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["action"], "approve")
        self.assertTrue(payload["success"])
        self.assertEqual(payload["run"]["decision"], "approved")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")
        self.assertEqual(payload["run"]["decision_note"], "Ship it.")
        self.assertTrue(payload["run"]["decided_at"])

    def test_reject_endpoint_updates_run_decision(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Reject run", repo_id="sample", actor_context=actor)
        service.finish_run(run.run_id, "failed")

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/reject",
                json={"note": "Please refine the proposed changes."},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["action"], "reject")
        self.assertTrue(payload["success"])
        self.assertEqual(payload["run"]["decision"], "rejected")
        self.assertEqual(payload["run"]["decided_by"], "lead-1")
        self.assertEqual(payload["run"]["decision_note"], "Please refine the proposed changes.")

    def test_approve_endpoint_rejects_incomplete_run_state(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Running run", repo_id="sample", actor_context=actor)

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        self.assertFalse(response.json()["success"])
        self.assertIn("only completed runs can be approved or rejected", response.json()["blocked_reason"])

    def test_approve_endpoint_blocks_no_changes_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("No changes run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "no_changes")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {"final_status": "no_changes"},
            },
            log_path=finished_run.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["success"])
        self.assertIn("Nothing to approve", payload["blocked_reason"])

    def test_approve_endpoint_blocks_validation_failed_implementation_run(self) -> None:
        service = RunService(storage_dir=self.storage_dir, persist=True)
        actor = ActorContext(
            actor_id="lead-1",
            actor_type="user",
            role="techlead",
            source_channel="api",
            display_name="Tech Lead",
        )
        run = service.start_run("Validation failed run", repo_id="sample", actor_context=actor)
        finished_run = service.finish_run(run.run_id, "partial")
        service.persist_run_detail(
            run.run_id,
            {
                "mode": "implement",
                "implementation_result": {"final_status": "candidate_validation_failed"},
                "validation_result": {"overall_status": "failed"},
            },
            log_path=finished_run.log_path,
        )

        with patch("web_app.root_agent.RunService", return_value=service):
            response = self.client.post(
                f"/runs/{run.run_id}/approve",
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["success"])
        self.assertIn("validation", payload["blocked_reason"].lower())

    def test_retry_endpoint_returns_child_run_with_retry_note(self) -> None:
        source_run = RunRecord(
            run_id="parent-run-1",
            goal="Implement update src/app.py",
            status="failed",
            started_at="2026-03-21T10:00:00+00:00",
            finished_at="2026-03-21T10:10:00+00:00",
            repo_id="sample",
            actor_context=ActorContext(
                actor_id="lead-1",
                actor_type="user",
                role="techlead",
                source_channel="api",
                display_name="Tech Lead",
            ),
        )
        child_run = RunRecord(
            run_id="child-run-1",
            goal="Implement update src/app.py",
            status="success",
            started_at="2026-03-21T10:11:00+00:00",
            finished_at="2026-03-21T10:15:00+00:00",
            attempt_index=2,
            total_attempts=3,
            parent_run_id="parent-run-1",
            repo_id="sample",
            actor_context=source_run.actor_context,
            retry_note="Try smaller change.",
            retry_context_summary="Validation failed: 2 test(s) failed",
            retry_context={"retry_reason": "validation_failed", "retry_strategy": "strict"},
        )
        with patch(
            "web_app.root_agent.run_root_agent",
            return_value=AgentResult(
                agent_name="runs",
                output_text="Retry executed.",
                success=True,
                task_intent="read",
                repo_context={},
                metadata={
                    "artifact_type": "run_retry",
                    "action": "retry",
                    "source_run": source_run,
                    "run_record": source_run,
                    "new_run_record": child_run,
                    "message": "Retry executed.",
                    "blocked_reason": "",
                    "success": True,
                },
            ),
        ), patch("web_app._artifact_run_service") as mocked_artifact_service:
            mocked_artifact_service.return_value.load_run_detail.return_value = RunDetail(
                run_id="child-run-1",
                mode="implement",
                goal="Implement update src/app.py",
                attempt_index=2,
                total_attempts=3,
                parent_run_id="parent-run-1",
                repo_id="sample",
                status="success",
                retry_note="Try smaller change.",
                retry_context_summary="Validation failed: 2 test(s) failed",
                retry_strategy="strict",
                retry_context={"retry_reason": "validation_failed", "retry_strategy": "strict"},
            )
            response = self.client.post(
                "/runs/parent-run-1/retry",
                json={"note": "Try smaller change.", "refinement_prompt": "Focus only on app.py"},
                headers={
                    "X-Actor-Id": "lead-1",
                    "X-Actor-Role": "techlead",
                    "X-Source-Channel": "api",
                },
            )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["success"])
        self.assertEqual(payload["new_run_id"], "child-run-1")
        self.assertEqual(payload["new_run"]["retry_note"], "Try smaller change.")
        self.assertEqual(payload["new_run"]["attempt_index"], 2)
        self.assertEqual(payload["new_run"]["total_attempts"], 3)
        self.assertEqual(payload["new_run"]["retry_strategy"], "strict")
        self.assertEqual(payload["new_run"]["retry_context"]["retry_reason"], "validation_failed")


if __name__ == "__main__":
    unittest.main()
