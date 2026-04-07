using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for AddOrderWithdrawView.xaml
    /// </summary>
    public partial class AddOrderWithdrawView
    {
        public AddOrderWithdrawView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { PaymentComboBoxEdit.Focus(); }));
        }
    }
}
