using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Directories.Category
{
    /// <summary>
    /// Interaction logic for CreateCategoryView.xaml
    /// </summary>
    public partial class CreateCategoryView
    {
        public CreateCategoryView()
        {
            InitializeComponent();
        }

        private void TextEditEditValueChanging(object sender, EditValueChangingEventArgs e)
        {
            if (sender is TextEdit textEdit)
            {
                textEdit.EditValue = (e.NewValue as string)?.ToLower();
                e.Handled = true;
            }
        }
    }
}
