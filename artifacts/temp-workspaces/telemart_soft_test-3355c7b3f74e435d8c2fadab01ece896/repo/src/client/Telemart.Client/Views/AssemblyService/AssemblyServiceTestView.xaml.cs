using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.AssemblyService
{
    /// <summary>
    /// Interaction logic for AssemblyServiceTestView.xaml
    /// </summary>
    public partial class AssemblyServiceTestView
    {
        public AssemblyServiceTestView()
        {
            InitializeComponent();
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            GridControl gridControl = sender as GridControl;
            TreeListView treeListView = gridControl?.View as TreeListView;

            treeListView?.ExpandAllNodes();
        }
    }
}
