using System.Windows.Input;
using DevExpress.Xpf.Bars;

namespace Telemart.Client.Views.Directories.Category
{
    /// <summary>
    /// Interaction logic for OptionActionsView.xaml
    /// </summary>
    public partial class CategoryOptionActionsView
    {
        public CategoryOptionActionsView()
        {
            InitializeComponent();
        }

        private void ImagePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenuManager.ShowElementContextMenu(sender);
        }
    }
}
