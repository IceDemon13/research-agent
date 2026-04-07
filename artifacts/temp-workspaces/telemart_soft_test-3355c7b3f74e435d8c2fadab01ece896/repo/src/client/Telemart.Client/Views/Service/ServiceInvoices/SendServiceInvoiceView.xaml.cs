using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Service.ServiceInvoices
{
    /// <summary>
    /// Interaction logic for SendServiceInvoiceView.xaml
    /// </summary>
    public partial class SendServiceInvoiceView
    {
        public SendServiceInvoiceView()
        {
            InitializeComponent();
        }

        private void SendServiceInvoiceViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => EmployeeCarrierComboBox.Focus()));
        }
    }
}
