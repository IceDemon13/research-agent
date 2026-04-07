using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Directories.Contractor
{
    /// <summary>
    /// Interaction logic for DirectoryContractorsView.xaml
    /// </summary>
    public partial class DirectoryContractorsView
    {
        public DirectoryContractorsView()
        {
            InitializeComponent();
        }

        private void TreeListViewRowDoubleClick(object sender, RowDoubleClickEventArgs e)
        {
            TreeListView listView = sender as TreeListView;

            if (listView == null)
            {
                return;
            }

            int rowHandle = e.HitInfo.RowHandle;

            TreeListNode node = listView.GetNodeByRowHandle(rowHandle);

            if (node.IsExpanded)
            {
                listView.CollapseNode(rowHandle);
            }
            else
            {
                listView.ExpandNode(rowHandle);
            }
        }
    }
}
