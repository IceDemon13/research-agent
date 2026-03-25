import json
import sqlite3
from contextlib import contextmanager
from pathlib import Path

from config import settings
from contracts.actor_contract import ActorContext
from contracts.permission_contract import PermissionDecision, PermissionScope, RolePolicy
from contracts.repo_metadata import RepoMetadata
from contracts.run_contract import RunRecord


def _timestamp() -> str:
    from datetime import datetime, timezone

    return datetime.now(timezone.utc).isoformat()


class DatabaseService:
    def __init__(
        self,
        *,
        dsn: str | None = None,
        pool_size: int | None = None,
    ) -> None:
        self._dsn = str(dsn if dsn is not None else settings.runtime.postgres_dsn).strip()
        self._pool_size = int(pool_size if pool_size is not None else settings.runtime.postgres_pool_size)
        self._backend = self._detect_backend(self._dsn)
        self._bootstrapped = False

    @property
    def enabled(self) -> bool:
        return bool(self._backend)

    @property
    def dsn(self) -> str:
        return self._dsn

    def bootstrap_schema(
        self,
        role_capability_map: dict[str, set[str]] | None = None,
        role_policy_map: dict[str, RolePolicy] | None = None,
        role_descriptions: dict[str, str] | None = None,
    ) -> None:
        if not self.enabled or self._bootstrapped:
            if self.enabled and role_capability_map:
                self.seed_role_capabilities(role_capability_map, role_policy_map, role_descriptions)
            return

        statements = [
            """
            CREATE TABLE IF NOT EXISTS users (
                user_id TEXT PRIMARY KEY,
                username TEXT NOT NULL DEFAULT '',
                display_name TEXT NOT NULL,
                email TEXT NOT NULL DEFAULT '',
                actor_type TEXT NOT NULL,
                role TEXT NOT NULL,
                role_name TEXT NOT NULL DEFAULT '',
                is_active INTEGER NOT NULL DEFAULT 1,
                must_change_password INTEGER NOT NULL DEFAULT 0,
                password_hash TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL DEFAULT '',
                last_login_at TEXT NOT NULL DEFAULT ''
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS roles (
                role_name TEXT PRIMARY KEY,
                description TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS role_capabilities (
                role_name TEXT NOT NULL,
                capability TEXT NOT NULL,
                PRIMARY KEY (role_name, capability)
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS role_policies (
                role_name TEXT PRIMARY KEY,
                repo_allowlist_json TEXT NOT NULL,
                repo_denylist_json TEXT NOT NULL,
                jira_project_allowlist_json TEXT NOT NULL,
                jira_project_denylist_json TEXT NOT NULL,
                protected_branch_prefixes_json TEXT NOT NULL,
                dry_run_only INTEGER NOT NULL,
                publication_requires_pr INTEGER NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS repos (
                repo_id TEXT PRIMARY KEY,
                root_path TEXT NOT NULL,
                local_path TEXT NOT NULL,
                remote_url TEXT NOT NULL,
                display_name TEXT NOT NULL,
                default_branch TEXT NOT NULL,
                status TEXT NOT NULL,
                indexed_at TEXT NOT NULL,
                index_status TEXT NOT NULL DEFAULT '',
                indexed_head TEXT NOT NULL DEFAULT '',
                index_error TEXT NOT NULL DEFAULT '',
                reindex_required INTEGER NOT NULL DEFAULT 0,
                sync_status TEXT NOT NULL DEFAULT '',
                last_sync_at TEXT NOT NULL DEFAULT '',
                sync_error TEXT NOT NULL DEFAULT '',
                intelligence_provider TEXT NOT NULL DEFAULT 'native',
                gitnexus_indexed INTEGER NOT NULL DEFAULT 0,
                gitnexus_indexed_at TEXT NOT NULL DEFAULT '',
                gitnexus_index_status TEXT NOT NULL DEFAULT '',
                gitnexus_index_error TEXT NOT NULL DEFAULT '',
                gitnexus_last_fallback_reason TEXT NOT NULL DEFAULT '',
                repo_group TEXT NOT NULL DEFAULT '',
                capability_tags_json TEXT NOT NULL DEFAULT '[]',
                historical_change_count INTEGER NOT NULL DEFAULT 0,
                historical_last_seen_at TEXT NOT NULL DEFAULT '',
                credential_alias TEXT NOT NULL DEFAULT '',
                auth_mode TEXT NOT NULL DEFAULT '',
                is_deleted INTEGER NOT NULL DEFAULT 0,
                deleted_at TEXT NOT NULL DEFAULT '',
                deleted_by TEXT NOT NULL DEFAULT '',
                delete_reason TEXT NOT NULL DEFAULT '',
                local_repo_state TEXT NOT NULL DEFAULT '',
                local_git_valid INTEGER NOT NULL DEFAULT 0,
                head_resolved INTEGER NOT NULL DEFAULT 0,
                recovered_by_reclone INTEGER NOT NULL DEFAULT 0,
                onboarding_last_error TEXT NOT NULL DEFAULT ''
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS historical_task (
                jira_key TEXT PRIMARY KEY,
                normalized_task_text TEXT NOT NULL DEFAULT '',
                task_snapshot_text TEXT NOT NULL DEFAULT '',
                updated_at TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS historical_change (
                change_id TEXT PRIMARY KEY,
                jira_key TEXT NOT NULL,
                repo_id TEXT NOT NULL,
                commit_hash TEXT NOT NULL,
                branch_name TEXT NOT NULL DEFAULT '',
                committed_at TEXT NOT NULL,
                created_at TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS historical_change_file (
                change_id TEXT NOT NULL,
                file_path TEXT NOT NULL,
                PRIMARY KEY (change_id, file_path)
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS runs (
                run_id TEXT PRIMARY KEY,
                goal TEXT NOT NULL,
                status TEXT NOT NULL,
                attempt_index INTEGER NOT NULL,
                total_attempts INTEGER NOT NULL,
                parent_run_id TEXT NOT NULL,
                actor_id TEXT NOT NULL,
                repo_id TEXT NOT NULL,
                started_at TEXT NOT NULL,
                finished_at TEXT NOT NULL,
                scm_branch TEXT NOT NULL,
                scm_commit TEXT NOT NULL,
                pr_url TEXT NOT NULL,
                review_url TEXT NOT NULL,
                decision TEXT NOT NULL,
                decided_at TEXT NOT NULL,
                decided_by TEXT NOT NULL,
                decision_note TEXT NOT NULL,
                retry_note TEXT NOT NULL,
                retry_context_summary TEXT NOT NULL,
                retry_context_json TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS run_steps (
                run_id TEXT NOT NULL,
                step_name TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at TEXT NOT NULL,
                finished_at TEXT NOT NULL,
                message TEXT NOT NULL,
                error_type TEXT NOT NULL,
                error_message TEXT NOT NULL,
                PRIMARY KEY (run_id, step_name)
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS policy_decisions (
                run_id TEXT NOT NULL,
                capability TEXT NOT NULL,
                actor_id TEXT NOT NULL,
                actor_role TEXT NOT NULL,
                allowed INTEGER NOT NULL,
                deny_reason_code TEXT NOT NULL,
                reason TEXT NOT NULL,
                scope_json TEXT NOT NULL,
                source TEXT NOT NULL,
                details_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                PRIMARY KEY (run_id, capability, reason, created_at)
            )
            """,
        ]
        with self._connection() as connection:
            cursor = connection.cursor()
            for statement in statements:
                cursor.execute(statement)
            self._ensure_column(
                cursor,
                "users",
                "username",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "users",
                "email",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "users",
                "role_name",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "users",
                "is_active",
                "INTEGER NOT NULL DEFAULT 1",
            )
            self._ensure_column(
                cursor,
                "users",
                "must_change_password",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "users",
                "password_hash",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "users",
                "updated_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "users",
                "last_login_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "roles",
                "description",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "local_path",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "remote_url",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "index_status",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "indexed_head",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "index_error",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "reindex_required",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "sync_status",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "last_sync_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "sync_error",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "intelligence_provider",
                "TEXT NOT NULL DEFAULT 'native'",
            )
            self._ensure_column(
                cursor,
                "repos",
                "gitnexus_indexed",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "gitnexus_indexed_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "gitnexus_index_status",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "gitnexus_index_error",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "gitnexus_last_fallback_reason",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "repo_group",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "capability_tags_json",
                "TEXT NOT NULL DEFAULT '[]'",
            )
            self._ensure_column(
                cursor,
                "repos",
                "historical_change_count",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "historical_last_seen_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "credential_alias",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "auth_mode",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "is_deleted",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "deleted_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "deleted_by",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "delete_reason",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "local_repo_state",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "repos",
                "local_git_valid",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "head_resolved",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "recovered_by_reclone",
                "INTEGER NOT NULL DEFAULT 0",
            )
            self._ensure_column(
                cursor,
                "repos",
                "onboarding_last_error",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "attempt_index",
                "INTEGER NOT NULL DEFAULT 1",
            )
            self._ensure_column(
                cursor,
                "runs",
                "total_attempts",
                "INTEGER NOT NULL DEFAULT 1",
            )
            self._ensure_column(
                cursor,
                "runs",
                "parent_run_id",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "decision",
                "TEXT NOT NULL DEFAULT 'pending'",
            )
            self._ensure_column(
                cursor,
                "runs",
                "decided_at",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "decided_by",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "decision_note",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "retry_note",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "retry_context_summary",
                "TEXT NOT NULL DEFAULT ''",
            )
            self._ensure_column(
                cursor,
                "runs",
                "retry_context_json",
                "TEXT NOT NULL DEFAULT '{}'",
            )
            self._ensure_unique_index(cursor, "idx_users_username", "users", "username")
            self._ensure_index(cursor, "idx_runs_started_at", "runs", "started_at")
            self._ensure_index(cursor, "idx_runs_status", "runs", "status")
            self._ensure_index(cursor, "idx_runs_actor_id", "runs", "actor_id")
            self._ensure_index(cursor, "idx_runs_repo_id", "runs", "repo_id")
            self._ensure_index(cursor, "idx_historical_change_repo_id", "historical_change", "repo_id")
            self._ensure_index(cursor, "idx_historical_change_jira_key", "historical_change", "jira_key")
            self._backfill_user_role_names(cursor)
        self._bootstrapped = True
        if role_capability_map:
            self.seed_role_capabilities(role_capability_map, role_policy_map, role_descriptions)

    def seed_role_capabilities(
        self,
        role_capability_map: dict[str, set[str]],
        role_policy_map: dict[str, RolePolicy] | None = None,
        role_descriptions: dict[str, str] | None = None,
    ) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        created_at = _timestamp()
        with self._connection() as connection:
            cursor = connection.cursor()
            for role_name, capabilities in sorted(role_capability_map.items()):
                cursor.execute(
                    self._sql(
                        """
                        INSERT INTO roles (role_name, description, created_at)
                        VALUES (?, ?, ?)
                        ON CONFLICT(role_name) DO UPDATE SET
                            description = excluded.description
                        """
                    ),
                    self._params(role_name, str((role_descriptions or {}).get(role_name, "") or "").strip(), created_at),
                )
                for capability in sorted(capabilities):
                    cursor.execute(
                        self._sql(
                            """
                            INSERT INTO role_capabilities (role_name, capability)
                            VALUES (?, ?)
                            ON CONFLICT(role_name, capability) DO NOTHING
                            """
                        ),
                        self._params(role_name, capability),
                    )
                role_policy = (role_policy_map or {}).get(role_name)
                if role_policy is not None:
                    cursor.execute(
                        self._sql(
                            """
                            INSERT INTO role_policies (
                                role_name, repo_allowlist_json, repo_denylist_json,
                                jira_project_allowlist_json, jira_project_denylist_json,
                                protected_branch_prefixes_json, dry_run_only, publication_requires_pr
                            )
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                            ON CONFLICT(role_name) DO UPDATE SET
                                repo_allowlist_json = excluded.repo_allowlist_json,
                                repo_denylist_json = excluded.repo_denylist_json,
                                jira_project_allowlist_json = excluded.jira_project_allowlist_json,
                                jira_project_denylist_json = excluded.jira_project_denylist_json,
                                protected_branch_prefixes_json = excluded.protected_branch_prefixes_json,
                                dry_run_only = excluded.dry_run_only,
                                publication_requires_pr = excluded.publication_requires_pr
                            """
                        ),
                        self._params(
                            role_name,
                            json.dumps(role_policy.repo_allowlist, ensure_ascii=False, sort_keys=True),
                            json.dumps(role_policy.repo_denylist, ensure_ascii=False, sort_keys=True),
                            json.dumps(role_policy.jira_project_allowlist, ensure_ascii=False, sort_keys=True),
                            json.dumps(role_policy.jira_project_denylist, ensure_ascii=False, sort_keys=True),
                            json.dumps(role_policy.protected_branch_prefixes, ensure_ascii=False, sort_keys=True),
                            1 if role_policy.dry_run_only else 0,
                            1 if role_policy.publication_requires_pr else 0,
                        ),
                    )

    def get_role_capabilities(self, role_name: str) -> set[str]:
        if not self.enabled:
            return set()
        self.bootstrap_schema()
        resolved_role = self.normalize_role_name(role_name)
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql("SELECT capability FROM role_capabilities WHERE role_name = ?"),
                self._params(resolved_role),
            )
            rows = cursor.fetchall()
        return {str(self._row_value(row, "capability") or "").strip() for row in rows}

    def get_role_policy(self, role_name: str) -> RolePolicy | None:
        resolved_role = self.normalize_role_name(role_name)
        row = self._fetch_one(
            """
            SELECT role_name, repo_allowlist_json, repo_denylist_json,
                   jira_project_allowlist_json, jira_project_denylist_json,
                   protected_branch_prefixes_json, dry_run_only, publication_requires_pr
            FROM role_policies
            WHERE role_name = ?
            """,
            self._params(resolved_role),
        )
        if row is None:
            return None
        return RolePolicy(
            role_name=str(row["role_name"] or "").strip(),
            repo_allowlist=self._load_json_list(row.get("repo_allowlist_json", "[]")),
            repo_denylist=self._load_json_list(row.get("repo_denylist_json", "[]")),
            jira_project_allowlist=self._load_json_list(row.get("jira_project_allowlist_json", "[]")),
            jira_project_denylist=self._load_json_list(row.get("jira_project_denylist_json", "[]")),
            protected_branch_prefixes=self._load_json_list(row.get("protected_branch_prefixes_json", "[]")),
            dry_run_only=bool(row.get("dry_run_only", 0)),
            publication_requires_pr=bool(row.get("publication_requires_pr", 0)),
        )

    def list_roles(self) -> list[dict]:
        return self._fetch_all(
            """
            SELECT role_name, description, created_at
            FROM roles
            ORDER BY role_name ASC
            """,
            self._params(),
        )

    def upsert_role(self, role_name: str, *, description: str = "") -> dict | None:
        if not self.enabled:
            return None
        self.bootstrap_schema()
        resolved_role = self.normalize_role_name(role_name)
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO roles (role_name, description, created_at)
                    VALUES (?, ?, ?)
                    ON CONFLICT(role_name) DO UPDATE SET
                        description = excluded.description
                    """
                ),
                self._params(resolved_role, str(description or "").strip(), _timestamp()),
            )
        return self._fetch_one(
            "SELECT role_name, description, created_at FROM roles WHERE role_name = ?",
            self._params(resolved_role),
        )

    def replace_role_capabilities(self, role_name: str, capabilities: list[str]) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        normalized_role = self.normalize_role_name(role_name)
        normalized_capabilities = sorted(
            {str(item or "").strip() for item in list(capabilities or []) if str(item or "").strip()}
        )
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(self._sql("DELETE FROM role_capabilities WHERE role_name = ?"), self._params(normalized_role))
            for capability in normalized_capabilities:
                cursor.execute(
                    self._sql(
                        "INSERT INTO role_capabilities (role_name, capability) VALUES (?, ?)"
                    ),
                    self._params(normalized_role, capability),
                )

    def upsert_role_policy(self, role_policy: RolePolicy) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO role_policies (
                        role_name, repo_allowlist_json, repo_denylist_json,
                        jira_project_allowlist_json, jira_project_denylist_json,
                        protected_branch_prefixes_json, dry_run_only, publication_requires_pr
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(role_name) DO UPDATE SET
                        repo_allowlist_json = excluded.repo_allowlist_json,
                        repo_denylist_json = excluded.repo_denylist_json,
                        jira_project_allowlist_json = excluded.jira_project_allowlist_json,
                        jira_project_denylist_json = excluded.jira_project_denylist_json,
                        protected_branch_prefixes_json = excluded.protected_branch_prefixes_json,
                        dry_run_only = excluded.dry_run_only,
                        publication_requires_pr = excluded.publication_requires_pr
                    """
                ),
                self._params(
                    str(role_policy.role_name or "").strip(),
                    json.dumps(role_policy.repo_allowlist, ensure_ascii=False, sort_keys=True),
                    json.dumps(role_policy.repo_denylist, ensure_ascii=False, sort_keys=True),
                    json.dumps(role_policy.jira_project_allowlist, ensure_ascii=False, sort_keys=True),
                    json.dumps(role_policy.jira_project_denylist, ensure_ascii=False, sort_keys=True),
                    json.dumps(role_policy.protected_branch_prefixes, ensure_ascii=False, sort_keys=True),
                    1 if role_policy.dry_run_only else 0,
                    1 if role_policy.publication_requires_pr else 0,
                ),
            )

    def list_role_policies(self) -> list[dict]:
        rows = self._fetch_all(
            """
            SELECT role_name, repo_allowlist_json, repo_denylist_json,
                   jira_project_allowlist_json, jira_project_denylist_json,
                   protected_branch_prefixes_json, dry_run_only, publication_requires_pr
            FROM role_policies
            ORDER BY role_name ASC
            """,
            self._params(),
        )
        for row in rows:
            row["repo_allowlist"] = self._load_json_list(row.pop("repo_allowlist_json", "[]"))
            row["repo_denylist"] = self._load_json_list(row.pop("repo_denylist_json", "[]"))
            row["jira_project_allowlist"] = self._load_json_list(row.pop("jira_project_allowlist_json", "[]"))
            row["jira_project_denylist"] = self._load_json_list(row.pop("jira_project_denylist_json", "[]"))
            row["protected_branch_prefixes"] = self._load_json_list(row.pop("protected_branch_prefixes_json", "[]"))
            row["dry_run_only"] = bool(row.get("dry_run_only", 0))
            row["publication_requires_pr"] = bool(row.get("publication_requires_pr", 0))
        return rows

    def has_role_capability_data(self) -> bool:
        if not self.enabled:
            return False
        row = self._fetch_one(
            "SELECT role_name FROM role_capabilities LIMIT 1",
            self._params(),
        )
        return row is not None

    def upsert_user(self, actor_context: ActorContext | None) -> None:
        if not self.enabled or actor_context is None:
            return
        self.bootstrap_schema()
        timestamp = _timestamp()
        resolved_role = self.normalize_role_name(actor_context.role)
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(user_id) DO UPDATE SET
                        username = CASE WHEN excluded.username <> '' THEN excluded.username ELSE users.username END,
                        display_name = excluded.display_name,
                        actor_type = excluded.actor_type,
                        role = excluded.role,
                        role_name = CASE WHEN excluded.role_name <> '' THEN excluded.role_name ELSE users.role_name END,
                        updated_at = excluded.updated_at
                    """
                ),
                self._params(
                    actor_context.actor_id,
                    str(actor_context.actor_id or "").strip(),
                    actor_context.display_name,
                    "",
                    actor_context.actor_type,
                    resolved_role,
                    resolved_role,
                    1,
                    0,
                    "",
                    timestamp,
                    timestamp,
                    "",
                ),
            )

    def fetch_user(self, actor_id: str) -> dict | None:
        return self._fetch_one(
            """
            SELECT user_id, username, display_name, email, actor_type, role, role_name,
                   is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
            FROM users
            WHERE user_id = ?
            """,
            self._params(str(actor_id or "").strip()),
        )

    def fetch_user_by_username(self, username: str) -> dict | None:
        return self._fetch_one(
            """
            SELECT user_id, username, display_name, email, actor_type, role, role_name,
                   is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
            FROM users
            WHERE LOWER(username) = ?
            """,
            self._params(str(username or "").strip().lower()),
        )

    def list_users(self) -> list[dict]:
        return self._fetch_all(
            """
            SELECT user_id, username, display_name, email, actor_type, role, role_name,
                   is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
            FROM users
            ORDER BY username ASC, user_id ASC
            """,
            self._params(),
        )

    def count_users(self) -> int:
        row = self._fetch_one("SELECT COUNT(*) AS user_count FROM users", self._params())
        return int((row or {}).get("user_count", 0) or 0)

    def create_or_update_directory_user(
        self,
        *,
        user_id: str,
        username: str,
        display_name: str,
        email: str,
        role_name: str,
        is_active: bool,
        must_change_password: bool,
        password_hash: str,
        actor_type: str = "user",
    ) -> dict:
        if not self.enabled:
            return {}
        self.bootstrap_schema()
        timestamp = _timestamp()
        resolved_role = self.normalize_role_name(role_name)
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO users (
                        user_id, username, display_name, email, actor_type, role, role_name,
                        is_active, must_change_password, password_hash, created_at, updated_at, last_login_at
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(user_id) DO UPDATE SET
                        username = excluded.username,
                        display_name = excluded.display_name,
                        email = excluded.email,
                        actor_type = excluded.actor_type,
                        role = excluded.role,
                        role_name = excluded.role_name,
                        is_active = excluded.is_active,
                        must_change_password = excluded.must_change_password,
                        password_hash = CASE WHEN excluded.password_hash <> '' THEN excluded.password_hash ELSE users.password_hash END,
                        updated_at = excluded.updated_at
                    """
                ),
                self._params(
                    str(user_id or "").strip(),
                    str(username or "").strip(),
                    str(display_name or "").strip(),
                    str(email or "").strip(),
                    str(actor_type or "user").strip() or "user",
                    resolved_role,
                    resolved_role,
                    1 if is_active else 0,
                    1 if must_change_password else 0,
                    str(password_hash or "").strip(),
                    timestamp,
                    timestamp,
                    "",
                ),
            )
        return self.fetch_user(str(user_id or "").strip()) or {}

    def update_user_directory_fields(
        self,
        user_id: str,
        *,
        display_name: str,
        email: str,
        role_name: str,
        is_active: bool,
        must_change_password: bool,
    ) -> dict | None:
        if not self.enabled:
            return None
        self.bootstrap_schema()
        resolved_role = self.normalize_role_name(role_name)
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    UPDATE users
                    SET display_name = ?, email = ?, role = ?, role_name = ?,
                        is_active = ?, must_change_password = ?, updated_at = ?
                    WHERE user_id = ?
                    """
                ),
                self._params(
                    str(display_name or "").strip(),
                    str(email or "").strip(),
                    resolved_role,
                    resolved_role,
                    1 if is_active else 0,
                    1 if must_change_password else 0,
                    _timestamp(),
                    str(user_id or "").strip(),
                ),
            )
        return self.fetch_user(user_id)

    def set_user_password_hash(self, user_id: str, password_hash: str, *, must_change_password: bool) -> dict | None:
        if not self.enabled:
            return None
        self.bootstrap_schema()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    UPDATE users
                    SET password_hash = ?, must_change_password = ?, updated_at = ?
                    WHERE user_id = ?
                    """
                ),
                self._params(
                    str(password_hash or "").strip(),
                    1 if must_change_password else 0,
                    _timestamp(),
                    str(user_id or "").strip(),
                ),
            )
        return self.fetch_user(user_id)

    def update_last_login(self, user_id: str) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        timestamp = _timestamp()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    UPDATE users
                    SET last_login_at = ?, updated_at = ?
                    WHERE user_id = ?
                    """
                ),
                self._params(timestamp, timestamp, str(user_id or "").strip()),
            )

    def upsert_repo(self, repo_metadata: RepoMetadata | None) -> None:
        if not self.enabled or repo_metadata is None:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO repos (
                        repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at,
                        index_status, indexed_head, index_error, reindex_required, sync_status, last_sync_at, sync_error,
                        intelligence_provider, gitnexus_indexed, gitnexus_indexed_at, gitnexus_index_status,
                        gitnexus_index_error, gitnexus_last_fallback_reason, repo_group, capability_tags_json,
                        historical_change_count, historical_last_seen_at, credential_alias, auth_mode,
                        is_deleted, deleted_at, deleted_by, delete_reason,
                        local_repo_state, local_git_valid, head_resolved, recovered_by_reclone, onboarding_last_error
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(repo_id) DO UPDATE SET
                        root_path = excluded.root_path,
                        local_path = excluded.local_path,
                        remote_url = excluded.remote_url,
                        display_name = excluded.display_name,
                        default_branch = excluded.default_branch,
                        status = excluded.status,
                        indexed_at = excluded.indexed_at,
                        index_status = excluded.index_status,
                        indexed_head = excluded.indexed_head,
                        index_error = excluded.index_error,
                        reindex_required = excluded.reindex_required,
                        sync_status = excluded.sync_status,
                        last_sync_at = excluded.last_sync_at,
                        sync_error = excluded.sync_error,
                        intelligence_provider = excluded.intelligence_provider,
                        gitnexus_indexed = excluded.gitnexus_indexed,
                        gitnexus_indexed_at = excluded.gitnexus_indexed_at,
                        gitnexus_index_status = excluded.gitnexus_index_status,
                        gitnexus_index_error = excluded.gitnexus_index_error,
                        gitnexus_last_fallback_reason = excluded.gitnexus_last_fallback_reason,
                        repo_group = excluded.repo_group,
                        capability_tags_json = excluded.capability_tags_json,
                        historical_change_count = excluded.historical_change_count,
                        historical_last_seen_at = excluded.historical_last_seen_at,
                        credential_alias = excluded.credential_alias,
                        auth_mode = excluded.auth_mode,
                        is_deleted = excluded.is_deleted,
                        deleted_at = excluded.deleted_at,
                        deleted_by = excluded.deleted_by,
                        delete_reason = excluded.delete_reason,
                        local_repo_state = excluded.local_repo_state,
                        local_git_valid = excluded.local_git_valid,
                        head_resolved = excluded.head_resolved,
                        recovered_by_reclone = excluded.recovered_by_reclone,
                        onboarding_last_error = excluded.onboarding_last_error
                    """
                ),
                self._params(
                    repo_metadata.repo_id,
                    repo_metadata.root_path,
                    repo_metadata.resolved_local_path,
                    repo_metadata.remote_url,
                    repo_metadata.display_name,
                    repo_metadata.default_branch,
                    repo_metadata.status,
                    repo_metadata.indexed_at,
                    repo_metadata.index_status,
                    repo_metadata.indexed_head,
                    repo_metadata.index_error,
                    1 if repo_metadata.reindex_required else 0,
                    repo_metadata.sync_status,
                    repo_metadata.last_sync_at,
                    repo_metadata.sync_error,
                    repo_metadata.intelligence_provider,
                    1 if repo_metadata.gitnexus_indexed else 0,
                    repo_metadata.gitnexus_indexed_at,
                    repo_metadata.gitnexus_index_status,
                    repo_metadata.gitnexus_index_error,
                    repo_metadata.gitnexus_last_fallback_reason,
                    repo_metadata.repo_group,
                    json.dumps(list(repo_metadata.capability_tags or []), ensure_ascii=False, sort_keys=True),
                    int(repo_metadata.historical_change_count or 0),
                    repo_metadata.historical_last_seen_at,
                    repo_metadata.credential_alias,
                    repo_metadata.auth_mode,
                    1 if repo_metadata.is_deleted else 0,
                    repo_metadata.deleted_at,
                    repo_metadata.deleted_by,
                    repo_metadata.delete_reason,
                    repo_metadata.local_repo_state,
                    1 if repo_metadata.local_git_valid else 0,
                    1 if repo_metadata.head_resolved else 0,
                    1 if repo_metadata.recovered_by_reclone else 0,
                    repo_metadata.onboarding_last_error,
                ),
            )

    def fetch_repo(self, repo_id: str) -> dict | None:
        row = self._fetch_one(
            """
            SELECT repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at,
                   index_status, indexed_head, index_error, reindex_required, sync_status, last_sync_at, sync_error,
                   intelligence_provider, gitnexus_indexed, gitnexus_indexed_at, gitnexus_index_status,
                   gitnexus_index_error, gitnexus_last_fallback_reason, repo_group, capability_tags_json,
                   historical_change_count, historical_last_seen_at, credential_alias, auth_mode,
                   is_deleted, deleted_at, deleted_by, delete_reason,
                   local_repo_state, local_git_valid, head_resolved, recovered_by_reclone, onboarding_last_error
            FROM repos
            WHERE repo_id = ?
            """,
            self._params(str(repo_id or "").strip()),
        )
        return self._normalize_repo_row(row) if row is not None else None

    def list_repos(self) -> list[dict]:
        if not self.enabled:
            return []
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql(
                    """
                    SELECT repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at,
                           index_status, indexed_head, index_error, reindex_required, sync_status, last_sync_at, sync_error,
                           intelligence_provider, gitnexus_indexed, gitnexus_indexed_at, gitnexus_index_status,
                           gitnexus_index_error, gitnexus_last_fallback_reason, repo_group, capability_tags_json,
                           historical_change_count, historical_last_seen_at, credential_alias, auth_mode,
                           is_deleted, deleted_at, deleted_by, delete_reason,
                           local_repo_state, local_git_valid, head_resolved, recovered_by_reclone, onboarding_last_error
                    FROM repos
                    ORDER BY repo_id
                    """
                )
            )
            rows = cursor.fetchall()
        return [self._normalize_repo_row(self._row_to_dict(row)) for row in rows]

    def upsert_historical_task(
        self,
        *,
        jira_key: str,
        normalized_task_text: str = "",
        task_snapshot_text: str = "",
    ) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO historical_task (jira_key, normalized_task_text, task_snapshot_text, updated_at)
                    VALUES (?, ?, ?, ?)
                    ON CONFLICT(jira_key) DO UPDATE SET
                        normalized_task_text = excluded.normalized_task_text,
                        task_snapshot_text = excluded.task_snapshot_text,
                        updated_at = excluded.updated_at
                    """
                ),
                self._params(
                    str(jira_key or "").strip().upper(),
                    str(normalized_task_text or "").strip(),
                    str(task_snapshot_text or "").strip(),
                    _timestamp(),
                ),
            )

    def replace_historical_changes_for_repo(self, repo_id: str, changes: list[dict]) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        resolved_repo_id = str(repo_id or "").strip()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql(
                    """
                    DELETE FROM historical_change_file
                    WHERE change_id IN (
                        SELECT change_id FROM historical_change WHERE repo_id = ?
                    )
                    """
                ),
                self._params(resolved_repo_id),
            )
            cursor.execute(
                self._sql("DELETE FROM historical_change WHERE repo_id = ?"),
                self._params(resolved_repo_id),
            )
            for item in list(changes or []):
                change = dict(item or {})
                change_id = str(change.get("change_id", "") or "").strip()
                if not change_id:
                    continue
                cursor.execute(
                    self._sql(
                        """
                        INSERT INTO historical_change (
                            change_id, jira_key, repo_id, commit_hash, branch_name, committed_at, created_at
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?)
                        """
                    ),
                    self._params(
                        change_id,
                        str(change.get("jira_key", "") or "").strip().upper(),
                        resolved_repo_id,
                        str(change.get("commit_hash", "") or "").strip(),
                        str(change.get("branch_name", "") or "").strip(),
                        str(change.get("committed_at", "") or "").strip(),
                        _timestamp(),
                    ),
                )
                for file_path in list(change.get("changed_files", []) or []):
                    normalized_path = str(file_path or "").strip()
                    if not normalized_path:
                        continue
                    cursor.execute(
                        self._sql(
                            """
                            INSERT INTO historical_change_file (change_id, file_path)
                            VALUES (?, ?)
                            ON CONFLICT(change_id, file_path) DO NOTHING
                            """
                        ),
                        self._params(change_id, normalized_path),
                    )

    def fetch_historical_tasks(self) -> list[dict]:
        return self._fetch_all(
            """
            SELECT jira_key, normalized_task_text, task_snapshot_text, updated_at
            FROM historical_task
            ORDER BY jira_key ASC
            """,
            self._params(),
        )

    def fetch_historical_changes(self, *, repo_id: str = "") -> list[dict]:
        if not self.enabled:
            return []
        self.bootstrap_schema()
        query = """
            SELECT c.change_id, c.jira_key, c.repo_id, c.commit_hash, c.branch_name, c.committed_at, c.created_at,
                   f.file_path
            FROM historical_change c
            LEFT JOIN historical_change_file f ON f.change_id = c.change_id
        """
        params: tuple = self._params()
        if str(repo_id or "").strip():
            query += " WHERE c.repo_id = ?"
            params = self._params(str(repo_id or "").strip())
        query += " ORDER BY c.committed_at DESC, c.change_id ASC, f.file_path ASC"
        rows = self._fetch_all(query, params)
        grouped: dict[str, dict] = {}
        for row in rows:
            change_id = str(row.get("change_id", "") or "").strip()
            if not change_id:
                continue
            entry = grouped.setdefault(
                change_id,
                {
                    "change_id": change_id,
                    "jira_key": str(row.get("jira_key", "") or "").strip(),
                    "repo_id": str(row.get("repo_id", "") or "").strip(),
                    "commit_hash": str(row.get("commit_hash", "") or "").strip(),
                    "branch_name": str(row.get("branch_name", "") or "").strip(),
                    "committed_at": str(row.get("committed_at", "") or "").strip(),
                    "created_at": str(row.get("created_at", "") or "").strip(),
                    "changed_files": [],
                },
            )
            file_path = str(row.get("file_path", "") or "").strip()
            if file_path:
                entry["changed_files"].append(file_path)
        return list(grouped.values())

    def upsert_run(self, run: RunRecord) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        actor_id = (
            run.actor_context.actor_id
            if run.actor_context is not None
            else ""
        )
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO runs (
                        run_id, goal, status, attempt_index, total_attempts, parent_run_id, actor_id, repo_id, started_at, finished_at,
                        scm_branch, scm_commit, pr_url, review_url, decision, decided_at, decided_by,
                        decision_note, retry_note, retry_context_summary, retry_context_json
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(run_id) DO UPDATE SET
                        goal = excluded.goal,
                        status = excluded.status,
                        attempt_index = excluded.attempt_index,
                        total_attempts = excluded.total_attempts,
                        parent_run_id = excluded.parent_run_id,
                        actor_id = excluded.actor_id,
                        repo_id = excluded.repo_id,
                        started_at = excluded.started_at,
                        finished_at = excluded.finished_at,
                        scm_branch = excluded.scm_branch,
                        scm_commit = excluded.scm_commit,
                        pr_url = excluded.pr_url,
                        review_url = excluded.review_url,
                        decision = excluded.decision,
                        decided_at = excluded.decided_at,
                        decided_by = excluded.decided_by,
                        decision_note = excluded.decision_note,
                        retry_note = excluded.retry_note,
                        retry_context_summary = excluded.retry_context_summary,
                        retry_context_json = excluded.retry_context_json
                    """
                ),
                self._params(
                    run.run_id,
                    run.goal,
                    run.status,
                    int(run.attempt_index or 1),
                    int(run.total_attempts or 1),
                    run.parent_run_id,
                    actor_id,
                    run.repo_id,
                    run.started_at,
                    run.finished_at,
                    str(run.scm.get("branch_name", "") or ""),
                    str(run.scm.get("commit_hash", "") or ""),
                    run.pr_url,
                    run.review_url,
                    str(run.decision or "pending"),
                    str(run.decided_at or ""),
                    str(run.decided_by or ""),
                    str(run.decision_note or ""),
                    str(run.retry_note or ""),
                    str(run.retry_context_summary or ""),
                    json.dumps(dict(run.retry_context or {}), ensure_ascii=False, sort_keys=True),
                ),
            )

    def replace_run_steps(self, run: RunRecord) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql("DELETE FROM run_steps WHERE run_id = ?"),
                self._params(run.run_id),
            )
            for step in list(run.steps):
                cursor.execute(
                    self._sql(
                        """
                        INSERT INTO run_steps (
                            run_id, step_name, status, started_at, finished_at,
                            message, error_type, error_message
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                        """
                    ),
                    self._params(
                        run.run_id,
                        step.name,
                        step.status,
                        step.started_at,
                        step.finished_at,
                        step.message,
                        step.error.type if step.error is not None else "",
                        step.error.message if step.error is not None else "",
                    ),
                )

    def replace_policy_decisions(self, run_id: str, decisions: list[PermissionDecision]) -> None:
        if not self.enabled:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql("DELETE FROM policy_decisions WHERE run_id = ?"),
                self._params(run_id),
            )
            for decision in list(decisions):
                cursor.execute(
                    self._sql(
                        """
                    INSERT INTO policy_decisions (
                            run_id, capability, actor_id, actor_role, allowed, deny_reason_code, reason,
                            scope_json, source, details_json, created_at
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                        """
                    ),
                    self._params(
                        run_id,
                        decision.capability,
                        decision.actor_id,
                        decision.actor_role,
                        1 if decision.allowed else 0,
                        decision.deny_reason_code,
                        decision.reason,
                        json.dumps(decision.scope.to_dict(), ensure_ascii=False, sort_keys=True),
                        decision.source,
                        json.dumps(decision.details, ensure_ascii=False, sort_keys=True),
                        _timestamp(),
                    ),
                )

    def fetch_run(self, run_id: str) -> dict | None:
        return self._fetch_one(
            """
            SELECT run_id, goal, status, parent_run_id, actor_id, repo_id, started_at, finished_at,
                   attempt_index, total_attempts,
                   scm_branch, scm_commit, pr_url, review_url, decision, decided_at, decided_by,
                   decision_note, retry_note, retry_context_summary, retry_context_json
            FROM runs
            WHERE run_id = ?
            """,
            self._params(str(run_id or "").strip()),
        )

    def fetch_runs(
        self,
        *,
        status: str = "",
        repo_id: str = "",
        actor_id: str = "",
        role: str = "",
    ) -> list[dict]:
        if not self.enabled:
            return []
        self.bootstrap_schema()
        clauses = []
        params: list[str] = []

        resolved_status = str(status or "").strip().lower()
        if resolved_status:
            clauses.append("runs.status = ?")
            params.append(resolved_status)

        resolved_repo_id = str(repo_id or "").strip()
        if resolved_repo_id:
            clauses.append("runs.repo_id = ?")
            params.append(resolved_repo_id)

        resolved_actor_id = str(actor_id or "").strip()
        if resolved_actor_id:
            clauses.append("runs.actor_id = ?")
            params.append(resolved_actor_id)

        resolved_role = str(role or "").strip().lower()
        if resolved_role:
            clauses.append("users.role = ?")
            params.append(resolved_role)

        where_clause = ""
        if clauses:
            where_clause = f"WHERE {' AND '.join(clauses)}"

        return self._fetch_all(
            f"""
              SELECT runs.run_id, runs.goal, runs.status, runs.attempt_index, runs.total_attempts, runs.parent_run_id, runs.actor_id, runs.repo_id,
                     runs.started_at, runs.finished_at, runs.scm_branch, runs.scm_commit,
                     runs.pr_url, runs.review_url, runs.decision, runs.decided_at, runs.decided_by,
                     runs.decision_note, runs.retry_note, runs.retry_context_summary, runs.retry_context_json,
                     users.username, users.display_name, users.role, users.role_name, users.actor_type
              FROM runs
            LEFT JOIN users ON users.user_id = runs.actor_id
            {where_clause}
            ORDER BY runs.started_at DESC, runs.run_id DESC
            """,
            self._params(*params),
        )

    def fetch_run_steps(self, run_id: str) -> list[dict]:
        return self._fetch_all(
            """
            SELECT run_id, step_name, status, started_at, finished_at, message, error_type, error_message
            FROM run_steps
            WHERE run_id = ?
            ORDER BY started_at ASC
            """,
            self._params(str(run_id or "").strip()),
        )

    def fetch_policy_decisions(self, run_id: str) -> list[dict]:
        rows = self._fetch_all(
            """
            SELECT run_id, capability, actor_id, actor_role, allowed, deny_reason_code, reason,
                   scope_json, source, details_json, created_at
            FROM policy_decisions
            WHERE run_id = ?
            ORDER BY created_at ASC
            """,
            self._params(str(run_id or "").strip()),
        )
        for row in rows:
            row["scope"] = self._load_json_dict(row.pop("scope_json", "{}"))
            row["details"] = self._load_json_dict(row.pop("details_json", "{}"))
        return rows

    def _fetch_one(self, sql: str, params: tuple) -> dict | None:
        if not self.enabled:
            return None
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(self._sql(sql), params)
            row = cursor.fetchone()
        return self._row_to_dict(row) if row is not None else None

    def _fetch_all(self, sql: str, params: tuple) -> list[dict]:
        if not self.enabled:
            return []
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(self._sql(sql), params)
            rows = cursor.fetchall()
        return [self._row_to_dict(row) for row in rows]

    @contextmanager
    def _connection(self):
        if self._backend == "sqlite":
            database_path = self._sqlite_path()
            database_path.parent.mkdir(parents=True, exist_ok=True)
            connection = sqlite3.connect(str(database_path))
            connection.row_factory = sqlite3.Row
        elif self._backend == "postgres":
            try:
                import psycopg2
                from psycopg2.extras import RealDictCursor
            except ImportError as exc:
                raise RuntimeError(
                    "psycopg2-binary is required for postgres_dsn connections. Install psycopg2-binary to enable Postgres metadata persistence."
                ) from exc
            connection = psycopg2.connect(self._dsn, cursor_factory=RealDictCursor)
        else:
            raise RuntimeError("Database service is not enabled.")

        try:
            yield connection
            connection.commit()
        finally:
            connection.close()

    def _sqlite_path(self) -> Path:
        dsn = self._dsn
        if dsn == "sqlite:///:memory:":
            return Path(":memory:")
        if dsn.startswith("sqlite:///"):
            return Path(dsn[len("sqlite:///"):]).expanduser().resolve()
        return Path("artifacts") / "metadata.db"

    @staticmethod
    def _detect_backend(dsn: str) -> str:
        if not dsn:
            return ""
        lowered = dsn.lower()
        if lowered.startswith("postgresql://") or lowered.startswith("postgres://"):
            return "postgres"
        if lowered.startswith("sqlite:///") or lowered == "sqlite:///:memory:":
            return "sqlite"
        raise ValueError(f"Unsupported metadata database DSN: {dsn}")

    def _sql(self, statement: str) -> str:
        if self._backend == "postgres":
            return statement.replace("?", "%s")
        return statement

    @staticmethod
    def _params(*values) -> tuple:
        return tuple(values)

    @staticmethod
    def _row_value(row, key: str):
        if isinstance(row, dict):
            return row.get(key)
        if isinstance(row, sqlite3.Row):
            return row[key]
        return row[key]

    @staticmethod
    def _row_to_dict(row) -> dict:
        if isinstance(row, dict):
            return dict(row)
        if isinstance(row, sqlite3.Row):
            return {key: row[key] for key in row.keys()}
        return dict(row)

    @classmethod
    def _normalize_repo_row(cls, row: dict | None) -> dict:
        payload = dict(row or {})
        payload["capability_tags"] = cls._load_json_list(payload.pop("capability_tags_json", "[]"))
        return payload

    @staticmethod
    def _load_json_list(value: str) -> list[str]:
        try:
            payload = json.loads(value or "[]")
        except json.JSONDecodeError:
            return []
        return [str(item).strip() for item in payload if str(item).strip()]

    @staticmethod
    def _load_json_dict(value: str) -> dict:
        try:
            payload = json.loads(value or "{}")
        except json.JSONDecodeError:
            return {}
        return payload if isinstance(payload, dict) else {}

    def _ensure_column(self, cursor, table_name: str, column_name: str, column_definition: str) -> None:
        if self._backend == "sqlite":
            cursor.execute(f"PRAGMA table_info({table_name})")
            existing_columns = {
                str(row["name"] if isinstance(row, sqlite3.Row) else row[1]).strip()
                for row in cursor.fetchall()
            }
            if column_name in existing_columns:
                return
            cursor.execute(f"ALTER TABLE {table_name} ADD COLUMN {column_name} {column_definition}")
            return

        if self._backend == "postgres":
            cursor.execute(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_name = %s AND column_name = %s
                """,
                (table_name, column_name),
            )
            if cursor.fetchone() is not None:
                return
            cursor.execute(f"ALTER TABLE {table_name} ADD COLUMN {column_name} {column_definition}")

    def _ensure_unique_index(self, cursor, index_name: str, table_name: str, column_name: str) -> None:
        if self._backend == "sqlite":
            cursor.execute(
                "SELECT name FROM sqlite_master WHERE type = 'index' AND name = ?",
                (index_name,),
            )
            if cursor.fetchone() is None:
                cursor.execute(
                    f"CREATE UNIQUE INDEX IF NOT EXISTS {index_name} ON {table_name}({column_name})"
                )
            return
        if self._backend == "postgres":
            cursor.execute(
                """
                SELECT indexname
                FROM pg_indexes
                WHERE tablename = %s AND indexname = %s
                """,
                (table_name, index_name),
            )
            if cursor.fetchone() is None:
                cursor.execute(
                    f"CREATE UNIQUE INDEX {index_name} ON {table_name}({column_name})"
                )

    def _ensure_index(self, cursor, index_name: str, table_name: str, column_name: str) -> None:
        if self._backend == "sqlite":
            cursor.execute(
                "SELECT name FROM sqlite_master WHERE type = 'index' AND name = ?",
                (index_name,),
            )
            if cursor.fetchone() is None:
                cursor.execute(
                    f"CREATE INDEX IF NOT EXISTS {index_name} ON {table_name}({column_name})"
                )
            return
        if self._backend == "postgres":
            cursor.execute(
                """
                SELECT indexname
                FROM pg_indexes
                WHERE tablename = %s AND indexname = %s
                """,
                (table_name, index_name),
            )
            if cursor.fetchone() is None:
                cursor.execute(
                    f"CREATE INDEX {index_name} ON {table_name}({column_name})"
                )

    def _backfill_user_role_names(self, cursor) -> None:
        cursor.execute(
            self._sql(
                """
                SELECT user_id, username, role, role_name, updated_at
                FROM users
                """
            )
        )
        for row in cursor.fetchall():
            user_id = str(self._row_value(row, "user_id") or "").strip()
            username = str(self._row_value(row, "username") or "").strip()
            legacy_role = str(self._row_value(row, "role") or "").strip()
            current_role_name = str(self._row_value(row, "role_name") or "").strip()
            current_updated_at = str(self._row_value(row, "updated_at") or "").strip()
            resolved_role = self.resolve_user_role_name(
                role_name=current_role_name,
                legacy_role=legacy_role,
                username=username,
            )
            if not user_id or not resolved_role:
                continue
            if legacy_role == resolved_role and current_role_name == resolved_role:
                continue
            cursor.execute(
                self._sql(
                    """
                    UPDATE users
                    SET role = ?, role_name = ?, updated_at = ?
                    WHERE user_id = ?
                    """
                ),
                self._params(
                    resolved_role,
                    resolved_role,
                    current_updated_at or _timestamp(),
                    user_id,
                ),
            )

    @staticmethod
    def normalize_role_name(role_name: str | None) -> str:
        resolved = str(role_name or "").strip().lower()
        if not resolved:
            return ""
        legacy_map = {
            "ba": "analyst",
        }
        return legacy_map.get(resolved, resolved)

    @classmethod
    def resolve_user_role_name(
        cls,
        *,
        role_name: str | None,
        legacy_role: str | None = None,
        username: str | None = None,
    ) -> str:
        resolved = cls.normalize_role_name(role_name or legacy_role)
        if resolved:
            return resolved
        bootstrap_username = str(settings.runtime.bootstrap_admin_username or "").strip().lower()
        if bootstrap_username and str(username or "").strip().lower() == bootstrap_username:
            return "admin"
        return ""
