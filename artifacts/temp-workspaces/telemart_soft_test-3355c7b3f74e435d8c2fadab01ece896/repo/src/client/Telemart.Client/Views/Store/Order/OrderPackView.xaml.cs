using System.Windows;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for OrderPackView.xaml
    /// </summary>
    public partial class OrderPackView
    {
        public OrderPackView()
        {
            InitializeComponent();
        }

        private void ButtonInfoOnClick(object sender, RoutedEventArgs e)
        {
            GridControl.View.HideEditor();
        }
    }
}
