using System;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Store.Invoice
{
    /// <summary>
    /// Interaction logic for CreateInvoiceView.xaml
    /// </summary>
    public partial class CreateInvoiceView
    {
        public CreateInvoiceView()
        {
            InitializeComponent();
        }

        private void ClearButtonOnClick(object sender, RoutedEventArgs e)
        {
            FocusTemplatesGrid();
        }

        private void FocusTemplatesGrid()
        {
            Action action = () => TemplateGridControl.Focus();
            Dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
        }

        private void SupplierEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            FocusTemplatesGrid();
        }
    }
}
