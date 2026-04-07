using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderSplitProductView.xaml
    /// </summary>
    public partial class OrderSplitProductView
    {
        public OrderSplitProductView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SplitQuantityComboBox.Focus()));
        }
    }
}
