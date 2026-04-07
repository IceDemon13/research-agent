using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Directories.ProductsFeatures
{
    /// <summary>
    /// Interaction logic for ProductFeaturesView.xaml.
    /// </summary>
    public partial class ProductFeaturesView
    {
        public ProductFeaturesView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableViewCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
