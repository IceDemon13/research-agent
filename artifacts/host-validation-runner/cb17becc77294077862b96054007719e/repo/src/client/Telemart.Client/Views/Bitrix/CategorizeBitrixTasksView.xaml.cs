using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Bitrix
{
    /// <summary>
    /// Interaction logic for CategorizeBitrixTasksView.xaml
    /// </summary>
    public partial class CategorizeBitrixTasksView
    {
        public CategorizeBitrixTasksView()
        {
            InitializeComponent();
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            using (TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, true))
            {
                while (nodeIterator.MoveNext())
                {
                    nodeIterator.Current.IsExpanded = nodeIterator.Current.ActualLevel < 1;
                }
            }
        }
    }
}
