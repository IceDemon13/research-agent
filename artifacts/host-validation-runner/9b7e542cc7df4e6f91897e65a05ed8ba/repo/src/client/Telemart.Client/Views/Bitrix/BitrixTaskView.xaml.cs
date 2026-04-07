using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Bitrix
{
    /// <summary>
    /// Interaction logic for BitrixTaskView.xaml
    /// </summary>
    public partial class BitrixTaskView
    {
        public BitrixTaskView()
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
