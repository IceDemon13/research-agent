using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for SelectDeliveryAddressView.xaml
    /// </summary>
    public partial class SelectDeliveryAddressView
    {
        public SelectDeliveryAddressView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => StreetComboBoxEdit.Focus()));
        }
    }
}
