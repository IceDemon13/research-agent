import json
import os

class Todo:
    def __init__(self, title, description):
        self.title = title
        self.description = description
        self.completed = False

    def to_dict(self):
        return {'title': self.title, 'description': self.description, 'completed': self.completed}

    @staticmethod
    def from_dict(data):
        todo = Todo(data['title'], data['description'])
        todo.completed = data['completed']
        return todo

class TodoApp:
    def __init__(self, filename='todos.json'):
        self.filename = filename
        self.todos = self.load_todos()

    def load_todos(self):
        if os.path.exists(self.filename):
            with open(self.filename, 'r') as f:
                todos_data = json.load(f)
                return [Todo.from_dict(todo) for todo in todos_data]
        return []

    def save_todos(self):
        with open(self.filename, 'w') as f:
            json.dump([todo.to_dict() for todo in self.todos], f)

    def add_todo(self, title, description):
        self.todos.append(Todo(title, description))
        self.save_todos()

    def list_todos(self):
        for todo in self.todos:
            print(f'Title: {todo.title}, Completed: {todo.completed}')

    def update_todo(self, index, title=None, description=None, completed=None):
        if index < 0 or index >= len(self.todos):
            print('Invalid index!')
            return
        if title is not None:
            self.todos[index].title = title
        if description is not None:
            self.todos[index].description = description
        if completed is not None:
            self.todos[index].completed = completed
        self.save_todos()

    def delete_todo(self, index):
        if index < 0 or index >= len(self.todos):
            print('Invalid index!')
            return
        del self.todos[index]
        self.save_todos()

if __name__ == '__main__':
    app = TodoApp()
    while True:
        command = input('Enter command (add/list/update/delete/quit): ').strip().lower()
        if command == 'add':
            title = input('Enter title: ')
            description = input('Enter description: ')
            app.add_todo(title, description)
        elif command == 'list':
            app.list_todos()
        elif command == 'update':
            index = int(input('Enter todo index to update: '))
            title = input('Enter new title (leave empty to keep current): ')
            description = input('Enter new description (leave empty to keep current): ')
            completed = input('Set completed status (true/false): ')
            completed = completed.lower() == 'true' if completed else None
            app.update_todo(index, title if title else None, description if description else None, completed)
        elif command == 'delete':
            index = int(input('Enter todo index to delete: '))
            app.delete_todo(index)
        elif command == 'quit':
            print('Exiting...')
            break
        else:
            print('Unknown command!')}}