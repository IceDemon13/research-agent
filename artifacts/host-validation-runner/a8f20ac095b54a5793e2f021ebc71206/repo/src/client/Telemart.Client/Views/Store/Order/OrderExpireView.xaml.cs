using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderExpireView.xaml
    /// </summary>
    public partial class OrderExpireView
    {
        public OrderExpireView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => WarehousesComboBox.Focus()));
        }
    }
}
