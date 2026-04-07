using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Directories.ProductsCatalog
{
    /// <summary>
    /// Interaction logic for ProductMoveView.xaml
    /// </summary>
    public partial class ProductMoveView
    {
        public ProductMoveView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
