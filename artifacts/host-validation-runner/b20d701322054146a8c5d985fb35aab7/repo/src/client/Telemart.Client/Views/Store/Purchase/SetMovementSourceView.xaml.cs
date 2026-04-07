using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Purchase
{
    /// <summary>
    /// Interaction logic for SetMovementSourceView.xaml
    /// </summary>
    public partial class SetMovementSourceView
    {
        public SetMovementSourceView()
        {
            InitializeComponent();
        }

        private void SetMovementSourceViewOnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => GridControl.Focus()));
        }
    }
}
