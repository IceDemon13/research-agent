using System;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store.Purchase
{
    /// <summary>
    /// Interaction logic for SetInvoiceSoureView.xaml
    /// </summary>
    public partial class SetInvoiceSourceView
    {
        public SetInvoiceSourceView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SuppliersComboBox.Focus()));
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => TableView.Focus()));
        }
    }
}
