using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Dialogs
{
    public partial class ConfirmView
    {
        public ConfirmView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { OkButton.Focus(); }));
        }
    }
}