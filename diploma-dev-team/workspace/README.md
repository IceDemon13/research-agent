# CLI Todo App Implementation

The CLI Todo App allows users to create, read, update, and delete todo items with a title, description, and completion status. Data is persisted to a JSON file to ensure todo items are maintained between application sessions. The implementation includes complete functionality for managing todo items, including methods to handle their creation, viewing, updating, deletion, and completion status management.

## Requirements
- The application must allow users to create, read, update, and delete todo items.
- Todo items should have at least a title, description, and completion status.
- Data must be persisted in a file to ensure todos are saved between sessions.
- User must be able to list all todo items, showing their title and completion status.
- The app should allow users to mark todos as complete or incomplete.

## Acceptance Criteria
- User can create a new todo item through the command line interface with a title and description.
- User can view all todo items listed with their current completion status.
- User can update an existing todo item's details such as title, description, or completion status.
- User can delete a todo item from the system through the CLI.
- All changes must be saved in a file and accurately reflect on restart of the application.

## Local Files
- src/__init__.py
- src/main.py
- tests/test_main.py
- requirements.txt
- README.md
