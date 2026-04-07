using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class TreeListControlCopyCurrentCellToClipboardBehavior : Behavior<TreeListControl>
    {
        private TreeListControl TreeListControl => AssociatedObject;

        protected override void OnAttached()
        {
            base.OnAttached();

            TreeListControl.CopyingToClipboard += TreeListControlCopyingToClipboard;
        }

        protected override void OnDetaching()
        {
            TreeListControl.CopyingToClipboard -= TreeListControlCopyingToClipboard;

            base.OnDetaching();
        }

        private void TreeListControlCopyingToClipboard(object sender, DevExpress.Xpf.Grid.TreeList.TreeListCopyingToClipboardEventArgs e)
        {
            string cellDisplayText = TreeListControl.GetCellDisplayText(e.Source.FocusedRowHandle, (TreeListColumn)TreeListControl.CurrentColumn);

            if (!string.IsNullOrEmpty(cellDisplayText))
            {
                Clipboard.SetDataObject(cellDisplayText);
            }

            e.Handled = true;
        }
    }
}