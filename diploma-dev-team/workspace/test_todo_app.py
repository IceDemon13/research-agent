import unittest
from todo_app import load_todos, save_todos, create_todo, update_todo, delete_todo, Todo
import os

todos_file = 'todos.json'

class TestTodoApp(unittest.TestCase):
    def setUp(self):
        self.todos = []
        if os.path.exists(todos_file):
            os.remove(todos_file)

    def test_create_todo(self):
        create_todo('Test Title', 'Test Description', self.todos)
        self.assertEqual(len(self.todos), 1)
        self.assertEqual(self.todos[0].title, 'Test Title')

    def test_update_todo(self):
        create_todo('Old Title', 'Old Description', self.todos)
        update_todo(0, title='New Title', description='New Description', completed=True, todos=self.todos)
        self.assertEqual(self.todos[0].title, 'New Title')
        self.assertTrue(self.todos[0].completed)

    def test_delete_todo(self):
        create_todo('Title', 'Description', self.todos)
        delete_todo(0, self.todos)
        self.assertEqual(len(self.todos), 0)

    def tearDown(self):
        if os.path.exists(todos_file):
            os.remove(todos_file)

if __name__ == '__main__':
    unittest.main()