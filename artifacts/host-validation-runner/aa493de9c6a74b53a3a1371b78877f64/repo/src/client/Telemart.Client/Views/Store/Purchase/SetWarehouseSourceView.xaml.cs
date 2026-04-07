using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Purchase
{
    /// <summary>
    /// Interaction logic for SetWarehouseSourceView.xaml
    /// </summary>
    public partial class SetWarehouseSourceView
    {
        public SetWarehouseSourceView()
        {
            InitializeComponent();
        }

        private void SetWarehouseSourceViewOnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => GridControl.Focus()));
        }
    }
}
