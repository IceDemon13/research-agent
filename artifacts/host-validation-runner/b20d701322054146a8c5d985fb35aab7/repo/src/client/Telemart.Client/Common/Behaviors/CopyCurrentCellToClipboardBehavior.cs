using System.Windows;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class CopyCurrentCellToClipboardBehavior : BehaviorBase<GridControl>
    {
        protected override void OnSetup()
        {
            AssociatedObject.CopyingToClipboard += GridCopyingToClipboard;
        }

        protected override void OnCleanup()
        {
            AssociatedObject.CopyingToClipboard -= GridCopyingToClipboard;
        }

        private void GridCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            string cellDisplayText = AssociatedObject.GetCellDisplayText(e.Source.FocusedRowHandle, (GridColumn)AssociatedObject.CurrentColumn);

            if (!string.IsNullOrEmpty(cellDisplayText))
            {
                Clipboard.SetDataObject(cellDisplayText);
            }

            e.Handled = true;
        }
    }
}