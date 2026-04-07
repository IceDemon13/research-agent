using DevExpress.Data;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using Telemart.Client.ViewModels.Store.Call;

namespace Telemart.Client.Views.Store.Call
{
    /// <summary>
    /// Interaction logic for StoreCallsView.xaml
    /// </summary>
    public partial class StoreCallsView
    {
        public StoreCallsView()
        {
            InitializeComponent();
        }

        private void TreeListViewOnCustomColumnSort(object sender, TreeListCustomColumnSortEventArgs e)
        {
            CallViewItem row1 = (CallViewItem)e.Node1.Content;
            CallViewItem row2 = (CallViewItem)e.Node2.Content;

            if (row1.IsFolder && !row2.IsFolder)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                e.Handled = true;
            }

            if (!row1.IsFolder && row2.IsFolder)
            {
                e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                e.Handled = true;
            }
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            if (GridControl.ItemsSource != null)
            {
                TreeListView?.ExpandAllNodes();
            }
        }
    }
}
