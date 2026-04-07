using System;
using System.Windows.Threading;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderEditPaymentView.xaml
    /// </summary>
    public partial class OrderEditPaymentView
    {
        public OrderEditPaymentView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => NewPaymentComboBox.Focus()));
        }
    }
}