using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Service.ServiceCenters
{
    /// <summary>
    /// Interaction logic for ServiceCenterView.xaml
    /// </summary>
    public partial class ServiceCenterView
    {
        public ServiceCenterView()
        {
            InitializeComponent();
        }

        private void ServiceCenterViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => TypeComboBox.Focus()));
        }
    }
}
