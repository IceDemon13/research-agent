using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Money.Refund
{
    /// <summary>
    /// Interaction logic for RefundView.xaml
    /// </summary>
    public partial class RefundView
    {
        public RefundView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => PaymentsComboBox.Focus()));
        }
    }
}
