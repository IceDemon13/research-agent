using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.Views.Directories.Category
{
    /// <summary>
    /// Interaction logic for CategoryMoveView.xaml
    /// </summary>
    public partial class CategoryMoveView
    {
        public CategoryMoveView()
        {
            InitializeComponent();

            CategoryMoveViewModel viewModel = (CategoryMoveViewModel)DataContext;
            viewModel.OnDataLoaded += OnDataLoaded;
        }

        private void OnDataLoaded()
        {
            CategoryTreeView.ExpandNodes(1);
        }
    }
}
