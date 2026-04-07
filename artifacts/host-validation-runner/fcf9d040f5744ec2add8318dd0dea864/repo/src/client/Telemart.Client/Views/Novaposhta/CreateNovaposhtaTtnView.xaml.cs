using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Novaposhta
{
    /// <summary>
    /// Interaction logic for CreateNovaposhtaTtnView.xaml
    /// </summary>
    public partial class CreateNovaposhtaTtnView
    {
        public CreateNovaposhtaTtnView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => NpContractorComboBoxEdit.Focus()));
        }
    }
}
