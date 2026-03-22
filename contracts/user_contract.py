from dataclasses import dataclass, field


@dataclass(slots=True)
class UserRecord:
    user_id: str
    username: str
    display_name: str
    role_name: str
    email: str = ""
    is_active: bool = True
    must_change_password: bool = False
    password_hash: str = ""
    created_at: str = ""
    updated_at: str = ""
    last_login_at: str = ""
    actor_type: str = "user"

    def to_safe_dict(self) -> dict:
        return {
            "user_id": self.user_id,
            "username": self.username,
            "display_name": self.display_name,
            "email": self.email,
            "role_name": self.role_name,
            "is_active": self.is_active,
            "must_change_password": self.must_change_password,
            "created_at": self.created_at,
            "updated_at": self.updated_at,
            "last_login_at": self.last_login_at,
            "actor_type": self.actor_type,
        }


@dataclass(slots=True)
class RoleRecord:
    role_name: str
    description: str = ""
    created_at: str = ""

    def to_dict(self) -> dict:
        return {
            "role_name": self.role_name,
            "description": self.description,
            "created_at": self.created_at,
        }


@dataclass(slots=True)
class PasswordActionResult:
    user: UserRecord
    generated_password: str = ""
    message: str = ""

    def to_dict(self) -> dict:
        payload = {
            "user": self.user.to_safe_dict(),
            "message": self.message,
        }
        if self.generated_password:
            payload["generated_password"] = self.generated_password
        return payload
