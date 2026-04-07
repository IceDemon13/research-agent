using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for DownloadPriceListView.xaml
    /// </summary>
    public partial class DownloadPriceListView
    {
        public DownloadPriceListView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => ContractorsComboBoxEdit.Focus()));
        }
    }
}
