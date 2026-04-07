using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Directories.ProductsFeatures
{
    /// <summary>
    /// Interaction logic for ProductFeaturesYandexMarketParseReportView.xaml
    /// </summary>
    public partial class ProductFeaturesYandexMarketParseReportView : UserControl
    {
        public ProductFeaturesYandexMarketParseReportView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
