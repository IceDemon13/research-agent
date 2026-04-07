using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for ProductSelectionView.xaml
    /// </summary>
    public partial class ProductSelectionView
    {
        public ProductSelectionView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { ProductsGrid.View.SearchControl.Focus(); }));
        }
    }
}
