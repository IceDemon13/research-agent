using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Novaposhta
{
    /// <summary>
    /// Interaction logic for AddNovaposhtaTtnView.xaml
    /// </summary>
    public partial class AddNovaposhtaTtnView
    {
        public AddNovaposhtaTtnView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => TrackNumberTextEdit.Focus()));
        }
    }
}
