using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class CloseWindowBehavior : Behavior<Window>
    {
        public static readonly DependencyProperty CloseTriggerProperty = DependencyProperty.Register(
            "CloseTrigger",
            typeof(bool),
            typeof(CloseWindowBehavior),
            new PropertyMetadata(false, OnCloseTriggerChanged));

        public bool CloseTrigger
        {
            get { return (bool)GetValue(CloseTriggerProperty); }
            set { SetValue(CloseTriggerProperty, value); }
        }

        private static void OnCloseTriggerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            CloseWindowBehavior behavior = dependencyObject as CloseWindowBehavior;
            behavior?.OnCloseTriggerChanged();
        }

        private void OnCloseTriggerChanged()
        {
            if (CloseTrigger)
            {
                AssociatedObject.Close();
            }
        }
    }
}