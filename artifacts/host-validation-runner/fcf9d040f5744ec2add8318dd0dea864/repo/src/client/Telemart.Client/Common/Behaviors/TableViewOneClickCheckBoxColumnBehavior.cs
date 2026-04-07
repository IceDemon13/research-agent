using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class TableViewOneClickCheckBoxColumnBehavior : BehaviorBase<TableView>
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            AssociatedObject.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
        }

        protected override void OnCleanup()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= PreviewMouseLeftButtonUp;

            base.OnCleanup();
        }

        private void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TableView view = sender as TableView;

            if (view == null)
            {
                return;
            }

            TableViewHitInfo hitInfo = view.CalcHitInfo(e.OriginalSource as DependencyObject);

            if (hitInfo.InRowCell && hitInfo.Column.FieldType == typeof(bool))
            {
                hitInfo.Column.Focus();
                view.ShowEditor();
            }
        }

        private void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TableView view = sender as TableView;

            if (view == null)
            {
                return;
            }

            if (view.Grid.CurrentColumn.FieldType == typeof(bool) && view.ActiveEditor != null)
            {
                view.ActiveEditor.EditValue = !(bool)view.ActiveEditor.EditValue;
                view.CloseEditor();
            }
        }
    }
}