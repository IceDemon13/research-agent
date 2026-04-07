using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.LogisticsAnalitics;

namespace Telemart.Client.Views.LogisticsAnalitics
{
    public partial class LogisticsAnaliticsView : UserControl
    {
        public LogisticsAnaliticsView()
        {
            InitializeComponent();
        }

        private void OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && (grid.CurrentItem is InvoiceLogisticsAnaliticsViewItem
                                               || grid.CurrentItem is MovementLogisticsAnaliticsViewItem
                                               || grid.CurrentItem is OrderLogisticsAnaliticsViewItem) && grid.CurrentCellValue != null)
            {
                string forClipboard = grid.CurrentCellValue is double || grid.CurrentCellValue is decimal ? $"{grid.CurrentCellValue:0.0}"
                    : grid.CurrentCellValue?.ToString();

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