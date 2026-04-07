using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Views.Store.Order
{
    public partial class ScheduleDeliveriesView : UserControl
    {
        public ScheduleDeliveriesView()
        {
            InitializeComponent();
        }

        private void GridControl_OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is ScheduleDeliveryViewItem)
            {
                string nameColumn = grid.CurrentColumn?.FieldName;

                string forClipboard = grid.CurrentCellValue?.ToString();

                if (nameColumn == nameof(ScheduleDeliveryViewItem.SelectedCourierDelivery) && grid.CurrentCellValue is CourierDeliveryViewItem)
                {
                    forClipboard = ((CourierDeliveryViewItem)grid.CurrentCellValue).DisplayText;
                }

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