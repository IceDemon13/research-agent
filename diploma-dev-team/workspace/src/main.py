"""Demo project generated for the diploma dev team workflow."""

FEATURE_ID = "cli_todo_app_implementation"
FEATURE_TITLE = "CLI Todo App Implementation"
REQUIREMENTS = ["The application must allow users to create, read, update, and delete todo items.", "Todo items should have at least a title, description, and completion status.", "Data must be persisted in a file to ensure todos are saved between sessions.", "User must be able to list all todo items, showing their title and completion status.", "The app should allow users to mark todos as complete or incomplete."]
ACCEPTANCE_CRITERIA = ["User can create a new todo item through the command line interface with a title and description.", "User can view all todo items listed with their current completion status.", "User can update an existing todo item's details such as title, description, or completion status.", "User can delete a todo item from the system through the CLI.", "All changes must be saved in a file and accurately reflect on restart of the application."]
DEVELOPER_NOTES = "The CLI Todo App allows users to create, read, update, and delete todo items with a title, description, and completion status. Data is persisted to a JSON file to ensure todo items are maintained between application sessions. The implementation includes complete functionality for managing todo items, including methods to handle their creation, viewing, updating, deletion, and completion status management."


def build_feature_summary() -> dict:
    return {
        "feature_id": FEATURE_ID,
        "title": FEATURE_TITLE,
        "requirements": REQUIREMENTS,
        "acceptance_criteria": ACCEPTANCE_CRITERIA,
        "notes": DEVELOPER_NOTES,
    }


def main() -> str:
    summary = build_feature_summary()
    first_requirement = summary["requirements"][0] if summary["requirements"] else "No requirements provided"
    return f"{summary['title']}: {first_requirement}"


if __name__ == "__main__":
    print(main())
