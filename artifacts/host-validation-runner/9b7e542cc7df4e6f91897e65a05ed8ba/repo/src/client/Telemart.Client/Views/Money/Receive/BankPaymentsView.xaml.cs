using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Money.Receive.BankPayment;

namespace Telemart.Client.Views.Money.Receive
{
    /// <summary>
    /// Interaction logic for BankPaymentsView.xaml
    /// </summary>
    public partial class BankPaymentsView
    {
        public BankPaymentsView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void OnCopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid && grid.CurrentItem is BankPaymentViewItem)
            {
                BankPaymentViewItem item = grid.CurrentItem as BankPaymentViewItem;

                string nameColumn = grid.CurrentColumn?.FieldName;

                string forClipboard = grid.CurrentCellValue?.ToString();

                if (nameColumn == nameof(BankPaymentViewItem.LastError))
                {
                    forClipboard = item?.LastError;
                }

                Clipboard.Clear();

                if (!string.IsNullOrEmpty(forClipboard))
                {
                    Clipboard.SetData(DataFormats.UnicodeText, forClipboard);
                }

                e.Handled = true;
            }
        }
    }
}