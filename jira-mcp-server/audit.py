from datetime import datetime


def audit_log(action: str, payload: dict, result: str = "ok") -> None:
    print(
        {
            "ts": datetime.utcnow().isoformat(),
            "action": action,
            "payload": payload,
            "result": result,
        }
    )