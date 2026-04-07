using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderEditContractorView.xaml
    /// </summary>
    public partial class OrderEditContractorView
    {
        public OrderEditContractorView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => NewContractorComboBox.Focus()));
        }
    }
}
