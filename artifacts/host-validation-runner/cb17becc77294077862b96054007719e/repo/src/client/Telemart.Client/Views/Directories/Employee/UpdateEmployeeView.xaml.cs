using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Directories.Employee;

namespace Telemart.Client.Views.Directories.Employee
{
    /// <summary>
    /// Interaction logic for UpdateEmployeeView.xaml.
    /// </summary>
    public partial class UpdateEmployeeView
    {
        public UpdateEmployeeView()
        {
            InitializeComponent();

            UpdateEmployeeViewModel viewModel = (UpdateEmployeeViewModel)DataContext;
            viewModel.OnEmployeeLocked += OnEmployeeLocked;
            viewModel.OnEmployeeUnlocked += OnEmployeeUnlocked;
        }

        private void GridControlOnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            using TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, false);

            while (nodeIterator.MoveNext())
            {
                TreeListNode currentNode = nodeIterator.Current;

                CategoryViewItem content = (CategoryViewItem)currentNode.Content;

                currentNode.IsCheckBoxEnabled = false;
                currentNode.IsExpanded = content.ParentLevel < 0;
            }
        }

        private void OnEmployeeLocked()
        {
            using TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, false);

            while (nodeIterator.MoveNext())
            {
                nodeIterator.Current.IsCheckBoxEnabled = true;
            }
        }

        private void OnEmployeeUnlocked()
        {
            using TreeListNodeIterator nodeIterator = new TreeListNodeIterator(TreeListView.Nodes, false);

            while (nodeIterator.MoveNext())
            {
                nodeIterator.Current.IsCheckBoxEnabled = false;
            }
        }

        private void BaseEditOnValidate(object sender, DevExpress.Xpf.Editors.ValidationEventArgs e)
        {
            e.IsValid = true;
        }
    }
}
