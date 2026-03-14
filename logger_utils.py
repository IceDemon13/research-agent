from pathlib import Path
from datetime import datetime


LOGS_DIR = Path("logs")
LOGS_DIR.mkdir(exist_ok=True)

LOG_FILE = LOGS_DIR / "agent.log"


def log_line(text: str) -> None:
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{timestamp}] {text}"

    print(line)

    with LOG_FILE.open("a", encoding="utf-8") as f:
        f.write(line + "\n")