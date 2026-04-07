using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.AdditionalServiceProduct;

namespace Telemart.Client.Views.AssemblyService
{
    /// <summary>
    /// Interaction logic for AssemblyServiceView.xaml
    /// </summary>
    public partial class AssemblyServiceView
    {
        public AssemblyServiceView()
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

        private void GridControl_OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is AdditionalServiceProductsViewItem)
            {
                string forClipboard = grid.CurrentCellValue?.ToString();

                Clipboard.Clear();

                if (!string.IsNullOrEmpty(forClipboard))
                {
                    Clipboard.SetText(forClipboard);
                }

                e.Handled = true;
            }
        }
    }
}