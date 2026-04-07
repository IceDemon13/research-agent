using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Warehouse.Inventory;

namespace Telemart.Client.Views.Warehouse.Inventory
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        private void OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is InventoryProductViewItem)
            {
                string forClipboard = grid.CurrentCellValue?.ToString();

                if (!string.IsNullOrEmpty(forClipboard))
                {
                    Clipboard.Clear();
                    Clipboard.SetData(DataFormats.UnicodeText, forClipboard);
                }

                e.Handled = true;
            }
        }
    }
}