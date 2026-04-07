using System;
using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using ICSharpCode.AvalonEdit;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class TextEditorContentBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty EditTextProperty = DependencyProperty.Register(
            nameof(EditText),
            typeof(string),
            typeof(TextEditorContentBehavior),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, EditTextChangedCallback));

        public string EditText
        {
            get => (string)GetValue(EditTextProperty);
            set => SetValue(EditTextProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
            }
        }

        private static void EditTextChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            TextEditorContentBehavior behavior = dependencyObject as TextEditorContentBehavior;

            TextEditor editor = behavior?.AssociatedObject;

            if (editor?.Document != null)
            {
                string text = eventArgs.NewValue.ToString();

                int caretOffset = editor.CaretOffset;
                editor.Document.Text = text;
                editor.CaretOffset = Math.Min(text.Length, caretOffset);
            }
        }

        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
        {
            if (sender is TextEditor editor && editor.Document != null)
            {
                EditText = editor.Document.Text;
            }
        }
    }
}