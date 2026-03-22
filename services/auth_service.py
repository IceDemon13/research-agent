import base64
import hashlib
import secrets
import uuid
import warnings

from contracts.actor_contract import ActorContext
from contracts.permission_contract import RolePolicy
from contracts.user_contract import PasswordActionResult, RoleRecord, UserRecord
from config import settings
from services.db_service import DatabaseService
from services.permission_service import ROLE_CAPABILITIES, ROLE_DESCRIPTIONS, ROLE_POLICIES


class AuthError(ValueError):
    pass


class PasswordManager:
    def __init__(self) -> None:
        self._argon2 = None
        self._exceptions = None
        try:
            from argon2 import PasswordHasher
            from argon2.exceptions import VerifyMismatchError

            self._argon2 = PasswordHasher()
            self._exceptions = (VerifyMismatchError,)
        except ImportError:
            self._argon2 = None
            self._exceptions = tuple()

    def hash_password(self, password: str) -> str:
        resolved = str(password or "")
        if self._argon2 is not None:
            return self._argon2.hash(resolved)
        salt = secrets.token_bytes(16)
        derived = hashlib.scrypt(
            resolved.encode("utf-8"),
            salt=salt,
            n=2**14,
            r=8,
            p=1,
            dklen=64,
        )
        return "scrypt$" + base64.b64encode(salt).decode("ascii") + "$" + base64.b64encode(derived).decode("ascii")

    def verify_password(self, password: str, password_hash: str) -> bool:
        resolved_hash = str(password_hash or "").strip()
        if not resolved_hash:
            return False
        if resolved_hash.startswith("$argon2") and self._argon2 is not None:
            try:
                return bool(self._argon2.verify(resolved_hash, str(password or "")))
            except self._exceptions:
                return False
        if resolved_hash.startswith("scrypt$"):
            try:
                _, salt_b64, digest_b64 = resolved_hash.split("$", 2)
                salt = base64.b64decode(salt_b64.encode("ascii"))
                expected = base64.b64decode(digest_b64.encode("ascii"))
                actual = hashlib.scrypt(
                    str(password or "").encode("utf-8"),
                    salt=salt,
                    n=2**14,
                    r=8,
                    p=1,
                    dklen=len(expected),
                )
                return secrets.compare_digest(actual, expected)
            except Exception:
                return False
        return False

    @staticmethod
    def generate_password(length: int = 20) -> str:
        alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*"
        return "".join(secrets.choice(alphabet) for _ in range(max(16, int(length or 20))))

    @staticmethod
    def validate_strength(password: str) -> None:
        resolved = str(password or "")
        if len(resolved) < 12:
            raise AuthError("Password must be at least 12 characters long.")
        if not any(ch.islower() for ch in resolved):
            raise AuthError("Password must include a lowercase letter.")
        if not any(ch.isupper() for ch in resolved):
            raise AuthError("Password must include an uppercase letter.")
        if not any(ch.isdigit() for ch in resolved):
            raise AuthError("Password must include a digit.")


class AuthService:
    def __init__(self, *, db_service: DatabaseService | None = None) -> None:
        self._db_service = db_service or DatabaseService()
        self._passwords = PasswordManager()
        self._warned_missing_admin = False
        if self._db_service.enabled:
            self._db_service.bootstrap_schema(ROLE_CAPABILITIES, ROLE_POLICIES, ROLE_DESCRIPTIONS)

    @property
    def enabled(self) -> bool:
        return self._db_service.enabled

    def bootstrap_admin_if_needed(self) -> None:
        if not self.enabled:
            return
        if self._db_service.count_users() > 0:
            if not self._has_admin_user() and not self._warned_missing_admin:
                warnings.warn(
                    "Users exist but no admin account is present. Configure bootstrap admin credentials or use the local password reset command.",
                    RuntimeWarning,
                    stacklevel=2,
                )
                self._warned_missing_admin = True
            return
        username = str(settings.runtime.bootstrap_admin_username or "").strip()
        password = str(settings.runtime.bootstrap_admin_password or "")
        if not username or not password:
            return
        self.create_user(
            username=username,
            display_name=str(settings.runtime.bootstrap_admin_display_name or "Bootstrap Admin").strip(),
            role_name="admin",
            email="",
            is_active=True,
            must_change_password=True,
            generate_password=False,
            password=password,
        )

    def authenticate(
        self,
        username: str,
        password: str,
        *,
        allow_dev_passwordless: bool = False,
    ) -> UserRecord:
        self.bootstrap_admin_if_needed()
        user_row = self._db_service.fetch_user_by_username(username)
        if user_row is None:
            raise AuthError("Invalid username or password.")
        user = self._user_from_row(user_row)
        if not user.is_active:
            raise AuthError("This user account is inactive.")
        passwordless_login = bool(allow_dev_passwordless and not str(password or "").strip())
        if not passwordless_login and not self._passwords.verify_password(password, user.password_hash):
            raise AuthError("Invalid username or password.")
        self._db_service.update_last_login(user.user_id)
        refreshed = self._db_service.fetch_user(user.user_id)
        return self._user_from_row(refreshed or user_row)

    def get_user(self, user_id: str) -> UserRecord | None:
        row = self._db_service.fetch_user(user_id)
        return self._user_from_row(row) if row is not None else None

    def get_user_by_username(self, username: str) -> UserRecord | None:
        row = self._db_service.fetch_user_by_username(username)
        return self._user_from_row(row) if row is not None else None

    def list_users(self) -> list[UserRecord]:
        return [self._user_from_row(row) for row in self._db_service.list_users()]

    def create_user(
        self,
        *,
        username: str,
        display_name: str,
        role_name: str,
        email: str = "",
        is_active: bool = True,
        must_change_password: bool = True,
        generate_password: bool = True,
        password: str = "",
    ) -> PasswordActionResult:
        normalized_username = str(username or "").strip()
        if not normalized_username:
            raise AuthError("Username is required.")
        if self._db_service.fetch_user_by_username(normalized_username) is not None:
            raise AuthError("Username already exists.")
        resolved_password = self._resolve_password(generate_password=generate_password, password=password)
        resolved_role_name = self._normalize_role_name(role_name) or "developer"
        user_row = self._db_service.create_or_update_directory_user(
            user_id=uuid.uuid4().hex,
            username=normalized_username,
            display_name=str(display_name or normalized_username).strip(),
            email=str(email or "").strip(),
            role_name=resolved_role_name,
            is_active=bool(is_active),
            must_change_password=bool(must_change_password),
            password_hash=self._passwords.hash_password(resolved_password),
        )
        user = self._user_from_row(user_row)
        return PasswordActionResult(
            user=user,
            generated_password=resolved_password if generate_password else "",
            message="User created.",
        )

    def update_user(
        self,
        user_id: str,
        *,
        display_name: str,
        email: str,
        role_name: str,
        is_active: bool,
        must_change_password: bool,
    ) -> UserRecord:
        resolved_role_name = self._normalize_role_name(role_name)
        updated = self._db_service.update_user_directory_fields(
            user_id,
            display_name=display_name,
            email=email,
            role_name=resolved_role_name,
            is_active=is_active,
            must_change_password=must_change_password,
        )
        if updated is None:
            raise AuthError("User not found.")
        return self._user_from_row(updated)

    def set_user_active(self, user_id: str, *, is_active: bool) -> UserRecord:
        existing = self.get_user(user_id)
        if existing is None:
            raise AuthError("User not found.")
        return self.update_user(
            user_id,
            display_name=existing.display_name,
            email=existing.email,
            role_name=existing.role_name,
            is_active=is_active,
            must_change_password=existing.must_change_password,
        )

    def reset_password(
        self,
        user_id: str,
        *,
        generate_password: bool = True,
        password: str = "",
        must_change_password: bool = True,
    ) -> PasswordActionResult:
        existing = self.get_user(user_id)
        if existing is None:
            raise AuthError("User not found.")
        resolved_password = self._resolve_password(generate_password=generate_password, password=password)
        updated = self._db_service.set_user_password_hash(
            user_id,
            self._passwords.hash_password(resolved_password),
            must_change_password=must_change_password,
        )
        return PasswordActionResult(
            user=self._user_from_row(updated or existing.to_safe_dict()),
            generated_password=resolved_password if generate_password else "",
            message="Password reset.",
        )

    def change_password(
        self,
        user_id: str,
        *,
        current_password: str,
        new_password: str,
        require_current_password: bool = True,
    ) -> UserRecord:
        existing = self.get_user(user_id)
        if existing is None:
            raise AuthError("User not found.")
        if require_current_password and not self._passwords.verify_password(current_password, existing.password_hash):
            raise AuthError("Current password is incorrect.")
        PasswordManager.validate_strength(new_password)
        updated = self._db_service.set_user_password_hash(
            user_id,
            self._passwords.hash_password(new_password),
            must_change_password=False,
        )
        return self._user_from_row(updated or existing.to_safe_dict())

    def list_roles(self) -> list[RoleRecord]:
        self.bootstrap_admin_if_needed()
        return [
            RoleRecord(
                role_name=str(row.get("role_name", "") or "").strip(),
                description=str(row.get("description", "") or "").strip(),
                created_at=str(row.get("created_at", "") or "").strip(),
            )
            for row in self._db_service.list_roles()
        ]

    def upsert_role(self, role_name: str, *, description: str = "") -> RoleRecord:
        row = self._db_service.upsert_role(self._normalize_role_name(role_name), description=description) or {}
        return RoleRecord(
            role_name=str(row.get("role_name", "") or "").strip(),
            description=str(row.get("description", "") or "").strip(),
            created_at=str(row.get("created_at", "") or "").strip(),
        )

    def get_role_capabilities(self, role_name: str) -> list[str]:
        return sorted(self._db_service.get_role_capabilities(self._normalize_role_name(role_name)))

    def set_role_capabilities(self, role_name: str, capabilities: list[str]) -> list[str]:
        resolved_role_name = self._normalize_role_name(role_name)
        self._db_service.replace_role_capabilities(resolved_role_name, capabilities)
        return self.get_role_capabilities(resolved_role_name)

    def list_role_policies(self) -> list[dict]:
        policies = self._db_service.list_role_policies()
        roles = {item.role_name: item for item in self.list_roles()}
        for row in policies:
            row["description"] = roles.get(str(row.get("role_name", "") or "").strip(), RoleRecord("")).description
        return policies

    def set_role_policy(self, role_name: str, payload: dict) -> dict:
        resolved_role_name = self._normalize_role_name(role_name)
        policy = RolePolicy(
            role_name=resolved_role_name,
            repo_allowlist=[str(item).strip() for item in list(payload.get("repo_allowlist", []) or []) if str(item).strip()],
            repo_denylist=[str(item).strip() for item in list(payload.get("repo_denylist", []) or []) if str(item).strip()],
            jira_project_allowlist=[str(item).strip().upper() for item in list(payload.get("jira_project_allowlist", []) or []) if str(item).strip()],
            jira_project_denylist=[str(item).strip().upper() for item in list(payload.get("jira_project_denylist", []) or []) if str(item).strip()],
            protected_branch_prefixes=[str(item).strip() for item in list(payload.get("protected_branch_prefixes", []) or []) if str(item).strip()],
            dry_run_only=bool(payload.get("dry_run_only", False)),
            publication_requires_pr=bool(payload.get("publication_requires_pr", False)),
        )
        self._db_service.upsert_role_policy(policy)
        stored = self._db_service.get_role_policy(resolved_role_name)
        return stored.to_dict() if stored is not None else policy.to_dict()

    def actor_context_for_user(self, user: UserRecord, *, source_channel: str = "web") -> ActorContext:
        policy = self._db_service.get_role_policy(user.role_name) if self._db_service.enabled else None
        return ActorContext(
            actor_id=user.user_id,
            actor_type=user.actor_type or "user",
            role=user.role_name,
            source_channel=str(source_channel or "web").strip() or "web",
            display_name=user.display_name,
            repo_allowlist=list(policy.repo_allowlist if policy is not None else []),
            repo_denylist=list(policy.repo_denylist if policy is not None else []),
            jira_project_allowlist=list(policy.jira_project_allowlist if policy is not None else []),
            jira_project_denylist=list(policy.jira_project_denylist if policy is not None else []),
            dry_run_only=bool(policy.dry_run_only if policy is not None else False),
        )

    def capability_summary(self, role_name: str) -> list[str]:
        return sorted(self._db_service.get_role_capabilities(self._normalize_role_name(role_name)))

    def reset_password_by_username(
        self,
        username: str,
        *,
        generate_password: bool = True,
        password: str = "",
        must_change_password: bool = True,
    ) -> PasswordActionResult:
        user = self.get_user_by_username(username)
        if user is None:
            raise AuthError("User not found.")
        return self.reset_password(
            user.user_id,
            generate_password=generate_password,
            password=password,
            must_change_password=must_change_password,
        )

    def _resolve_password(self, *, generate_password: bool, password: str) -> str:
        if generate_password:
            return self._passwords.generate_password()
        PasswordManager.validate_strength(password)
        return str(password or "")

    def _has_admin_user(self) -> bool:
        return any(self._normalize_role_name(user.role_name) == "admin" for user in self.list_users())

    @staticmethod
    def _normalize_role_name(
        role_name: str | None,
        legacy_role: str | None = None,
        *,
        username: str | None = None,
    ) -> str:
        return DatabaseService.resolve_user_role_name(
            role_name=role_name,
            legacy_role=legacy_role,
            username=username,
        )

    @staticmethod
    def _user_from_row(row: dict) -> UserRecord:
        resolved_role_name = AuthService._normalize_role_name(
            row.get("role_name", ""),
            row.get("role", ""),
            username=row.get("username", ""),
        )
        return UserRecord(
            user_id=str(row.get("user_id", "") or "").strip(),
            username=str(row.get("username", "") or "").strip(),
            display_name=str(row.get("display_name", "") or "").strip(),
            email=str(row.get("email", "") or "").strip(),
            role_name=resolved_role_name,
            is_active=bool(row.get("is_active", 1)),
            must_change_password=bool(row.get("must_change_password", 0)),
            password_hash=str(row.get("password_hash", "") or "").strip(),
            created_at=str(row.get("created_at", "") or "").strip(),
            updated_at=str(row.get("updated_at", "") or "").strip(),
            last_login_at=str(row.get("last_login_at", "") or "").strip(),
            actor_type=str(row.get("actor_type", "user") or "").strip() or "user",
        )
