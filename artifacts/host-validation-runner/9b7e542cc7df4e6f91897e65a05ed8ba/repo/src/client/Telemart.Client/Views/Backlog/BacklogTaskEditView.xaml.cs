using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Backlog;

namespace Telemart.Client.Views.Backlog
{
    /// <summary>
    /// Логика взаимодействия для BacklogTaskEditView.xaml.
    /// </summary>
    public partial class BacklogTaskEditView
    {
        public BacklogTaskEditView()
        {
            InitializeComponent();

            BacklogTaskEditViewModel viewModel = (BacklogTaskEditViewModel)DataContext;
            viewModel.OnEmployeeLocked += () => ChangeEnablingGridControl(true);
            viewModel.OnEmployeeUnlocked += () => ChangeEnablingGridControl(false);
        }

        private void CategoriesGridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            using (TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, true))
            {
                while (nodeIterator.MoveNext())
                {
                    nodeIterator.Current.IsExpanded = nodeIterator.Current.ActualLevel < 2;
                    nodeIterator.Current.IsCheckBoxEnabled = false;
                }
            }
        }

        private void ChangeEnablingGridControl(bool enable)
        {
            using (TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, false))
            {
                while (nodeIterator.MoveNext())
                {
                    nodeIterator.Current.IsCheckBoxEnabled = enable;
                }
            }
        }
    }
}
