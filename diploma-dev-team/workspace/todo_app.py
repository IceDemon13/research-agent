import json
import os

class Todo:
    def __init__(self, title, description, completed=False):
        self.title = title
        self.description = description
        self.completed = completed

    def to_dict(self):
        return {
            'title': self.title,
            'description': self.description,
            'completed': self.completed
        }

class TodoApp:
    def __init__(self, filename='todos.json'):
        self.filename = filename
        self.todos = self.load_todos()

    def load_todos(self):
        if os.path.exists(self.filename):
            with open(self.filename, 'r') as f:
                todos_data = json.load(f)
                return [Todo(**todo) for todo in todos_data]
        return []

    def save_todos(self):
        with open(self.filename, 'w') as f:
            json.dump([todo.to_dict() for todo in self.todos], f, indent=4)

    def create_todo(self, title, description):
        todo = Todo(title, description)
        self.todos.append(todo)
        self.save_todos()

    def read_todos(self):
        for todo in self.todos:
            status = "Completed" if todo.completed else "Incomplete"
            print(f"{todo.title} - {status}")

    def update_todo(self, title, new_title=None, new_description=None, new_status=None):
        for todo in self.todos:
            if todo.title == title:
                if new_title:
                    todo.title = new_title
                if new_description:
                    todo.description = new_description
                if new_status is not None:
                    todo.completed = new_status
                self.save_todos()
                return
        print("Todo not found.")

    def delete_todo(self, title):
        self.todos = [todo for todo in self.todos if todo.title != title]
        self.save_todos()

    def mark_complete(self, title):
        self.update_todo(title, new_status=True)

    def mark_incomplete(self, title):
        self.update_todo(title, new_status=False)

if __name__ == '__main__':
    app = TodoApp()
    # Example user interface for the CLI
    app.create_todo('Buy groceries', 'Milk, Bread, Cheese')  # Example command
    app.read_todos()  
    app.mark_complete('Buy groceries')  # Example command to update
    app.read_todos()  
    app.delete_todo('Buy groceries')  # Example command to delete
    app.read_todos()