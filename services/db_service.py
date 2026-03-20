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
    ) -> None:
        if not self.enabled or self._bootstrapped:
            if self.enabled and role_capability_map:
                self.seed_role_capabilities(role_capability_map, role_policy_map)
            return

        statements = [
            """
            CREATE TABLE IF NOT EXISTS users (
                user_id TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                actor_type TEXT NOT NULL,
                role TEXT NOT NULL,
                created_at TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS roles (
                role_name TEXT PRIMARY KEY,
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
                indexed_at TEXT NOT NULL
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS runs (
                run_id TEXT PRIMARY KEY,
                goal TEXT NOT NULL,
                status TEXT NOT NULL,
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
                decided_by TEXT NOT NULL
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
        self._bootstrapped = True
        if role_capability_map:
            self.seed_role_capabilities(role_capability_map, role_policy_map)

    def seed_role_capabilities(
        self,
        role_capability_map: dict[str, set[str]],
        role_policy_map: dict[str, RolePolicy] | None = None,
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
                        INSERT INTO roles (role_name, created_at)
                        VALUES (?, ?)
                        ON CONFLICT(role_name) DO NOTHING
                        """
                    ),
                    self._params(role_name, created_at),
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
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql("SELECT capability FROM role_capabilities WHERE role_name = ?"),
                self._params(str(role_name or "").strip()),
            )
            rows = cursor.fetchall()
        return {str(self._row_value(row, "capability") or "").strip() for row in rows}

    def get_role_policy(self, role_name: str) -> RolePolicy | None:
        row = self._fetch_one(
            """
            SELECT role_name, repo_allowlist_json, repo_denylist_json,
                   jira_project_allowlist_json, jira_project_denylist_json,
                   protected_branch_prefixes_json, dry_run_only, publication_requires_pr
            FROM role_policies
            WHERE role_name = ?
            """,
            self._params(str(role_name or "").strip()),
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
        created_at = _timestamp()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO users (user_id, display_name, actor_type, role, created_at)
                    VALUES (?, ?, ?, ?, ?)
                    ON CONFLICT(user_id) DO UPDATE SET
                        display_name = excluded.display_name,
                        actor_type = excluded.actor_type,
                        role = excluded.role
                    """
                ),
                self._params(
                    actor_context.actor_id,
                    actor_context.display_name,
                    actor_context.actor_type,
                    actor_context.role,
                    created_at,
                ),
            )

    def fetch_user(self, actor_id: str) -> dict | None:
        return self._fetch_one(
            "SELECT user_id, display_name, actor_type, role, created_at FROM users WHERE user_id = ?",
            self._params(str(actor_id or "").strip()),
        )

    def upsert_repo(self, repo_metadata: RepoMetadata | None) -> None:
        if not self.enabled or repo_metadata is None:
            return
        self.bootstrap_schema()
        with self._connection() as connection:
            connection.cursor().execute(
                self._sql(
                    """
                    INSERT INTO repos (repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(repo_id) DO UPDATE SET
                        root_path = excluded.root_path,
                        local_path = excluded.local_path,
                        remote_url = excluded.remote_url,
                        display_name = excluded.display_name,
                        default_branch = excluded.default_branch,
                        status = excluded.status,
                        indexed_at = excluded.indexed_at
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
                ),
            )

    def fetch_repo(self, repo_id: str) -> dict | None:
        return self._fetch_one(
            """
            SELECT repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at
            FROM repos
            WHERE repo_id = ?
            """,
            self._params(str(repo_id or "").strip()),
        )

    def list_repos(self) -> list[dict]:
        if not self.enabled:
            return []
        self.bootstrap_schema()
        with self._connection() as connection:
            cursor = connection.cursor()
            cursor.execute(
                self._sql(
                    """
                    SELECT repo_id, root_path, local_path, remote_url, display_name, default_branch, status, indexed_at
                    FROM repos
                    ORDER BY repo_id
                    """
                )
            )
            rows = cursor.fetchall()
        return [self._row_to_dict(row) for row in rows]

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
                        run_id, goal, status, parent_run_id, actor_id, repo_id, started_at, finished_at,
                        scm_branch, scm_commit, pr_url, review_url, decision, decided_at, decided_by
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT(run_id) DO UPDATE SET
                        goal = excluded.goal,
                        status = excluded.status,
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
                        decided_by = excluded.decided_by
                    """
                ),
                self._params(
                    run.run_id,
                    run.goal,
                    run.status,
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
                   scm_branch, scm_commit, pr_url, review_url, decision, decided_at, decided_by
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
            SELECT runs.run_id, runs.goal, runs.status, runs.parent_run_id, runs.actor_id, runs.repo_id,
                   runs.started_at, runs.finished_at, runs.scm_branch, runs.scm_commit,
                   runs.pr_url, runs.review_url, runs.decision, runs.decided_at, runs.decided_by,
                   users.display_name, users.role, users.actor_type
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
                import psycopg
            except ImportError as exc:
                raise RuntimeError(
                    "psycopg is required for postgres_dsn connections. Install psycopg to enable Postgres metadata persistence."
                ) from exc
            connection = psycopg.connect(self._dsn)
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
        if isinstance(row, sqlite3.Row):
            return row[key]
        return row[key]

    @staticmethod
    def _row_to_dict(row) -> dict:
        if isinstance(row, sqlite3.Row):
            return {key: row[key] for key in row.keys()}
        return dict(row)

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
