using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for StoreOrdersPackView.xaml
    /// </summary>
    public partial class StoreOrdersPackView
    {
        public StoreOrdersPackView()
        {
            InitializeComponent();
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_OnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void GridCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is OrderPackViewItem)
            {
                string nameColumn = grid.CurrentColumn?.FieldName;

                if (nameColumn == nameof(OrderPackViewItem.PackListId))
                {
                    string val = grid.CurrentCellValue?.ToString();

                    if (!string.IsNullOrEmpty(val))
                    {
                        Clipboard.SetData(DataFormats.UnicodeText, val);
                    }
                }
            }

            e.Handled = true;
        }
    }
}