namespace Telemart.Client.Views.Directories.Category
{
    /// <summary>
    /// Interaction logic for DirectoryCategoriesView.xaml
    /// </summary>
    public partial class DirectoryCategoriesView
    {
        public DirectoryCategoriesView()
        {
            InitializeComponent();
        }

        private void TreeListView_CustomNodeFilter(object sender, DevExpress.Xpf.Grid.TreeList.TreeListNodeFilterEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Visible = false;
                e.Node.IsExpanded = true;
                e.Handled = true;
            }
        }
    }
}
