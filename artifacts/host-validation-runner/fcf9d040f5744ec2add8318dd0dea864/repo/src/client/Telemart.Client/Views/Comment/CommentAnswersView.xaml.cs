namespace Telemart.Client.Views.Comment
{
    /// <summary>
    /// Interaction logic for CommentAnswersViewModel.xaml
    /// </summary>
    public partial class CommentAnswersView
    {
        public CommentAnswersView()
        {
            InitializeComponent();
        }

        private void Product_OnRequestNavigation(object sender, DevExpress.Xpf.Editors.HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://telemart.ua/products/{e.NavigationUrl}";
            e.Handled = true;
        }
    }
}
