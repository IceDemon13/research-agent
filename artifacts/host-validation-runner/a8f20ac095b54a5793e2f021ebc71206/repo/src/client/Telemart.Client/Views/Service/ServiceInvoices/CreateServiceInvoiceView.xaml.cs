using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Service.ServiceInvoices
{
    /// <summary>
    /// Interaction logic for CreateServiceInvoice.xaml
    /// </summary>
    public partial class CreateServiceInvoiceView
    {
        public CreateServiceInvoiceView()
        {
            InitializeComponent();
        }

        private void CreateServiceInvoiceLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => ServiceCentersComboBox.Focus()));
        }
    }
}
