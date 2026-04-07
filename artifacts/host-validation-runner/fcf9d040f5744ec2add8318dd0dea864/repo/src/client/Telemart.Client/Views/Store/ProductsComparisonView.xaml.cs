using System.Windows;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for ProductsComparisonView.xaml
    /// </summary>
    public partial class ProductsComparisonView
    {
        public ProductsComparisonView()
        {
            InitializeComponent();
        }

        private void ButtonInfoOnClick(object sender, RoutedEventArgs e)
        {
            GridControl.View.HideEditor();
        }
    }
}
