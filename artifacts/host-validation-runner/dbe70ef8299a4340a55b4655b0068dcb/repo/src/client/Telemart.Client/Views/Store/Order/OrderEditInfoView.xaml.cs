using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderEditInfoView.xaml
    /// </summary>
    public partial class OrderEditInfoView
    {
        public OrderEditInfoView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => ReceiveTimeDateEdit.Focus()));
        }
    }
}
