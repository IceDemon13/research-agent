using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for DelayedConfirmView.xaml
    /// </summary>
    public partial class DelayedConfirmView
    {
        public DelayedConfirmView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { CancelButton.Focus(); }));
        }
    }
}
