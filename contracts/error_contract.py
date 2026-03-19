from dataclasses import dataclass, field


@dataclass(slots=True)
class ExecutionError:
    type: str
    message: str
    step: str
    details: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        return {
            "type": self.type,
            "message": self.message,
            "step": self.step,
            "details": dict(self.details),
        }
