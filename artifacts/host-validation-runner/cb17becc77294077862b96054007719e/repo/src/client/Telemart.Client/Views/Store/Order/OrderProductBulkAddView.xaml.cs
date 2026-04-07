using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderProductBulkAddView.xaml
    /// </summary>
    public partial class OrderProductBulkAddView
    {
        public OrderProductBulkAddView()
        {
            InitializeComponent();
        }

        private void BulkAddViewOnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => RawTextEdit.Focus()));
        }
    }
}
