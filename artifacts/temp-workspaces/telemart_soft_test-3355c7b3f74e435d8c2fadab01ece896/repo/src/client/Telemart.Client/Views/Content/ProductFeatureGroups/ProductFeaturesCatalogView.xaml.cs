using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Content.ProductFeatureGroups
{
    /// <summary>
    /// Interaction logic for ProductFeaturesCatalogView.xaml
    /// </summary>
    public partial class ProductFeaturesCatalogView
    {
        public ProductFeaturesCatalogView()
        {
            InitializeComponent();
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            TreeListView treeListView = (sender as GridControl)?.View as TreeListView;

            treeListView?.ExpandAllNodes();
        }
    }
}
