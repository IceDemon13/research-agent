using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Store.ReturnInvoice
{
    /// <summary>
    /// Interaction logic for StoreInvoicesView.xaml
    /// </summary>
    public partial class StoreReturnInvoicesView
    {
        public StoreReturnInvoicesView()
        {
            InitializeComponent();
        }

        private void BitrixId_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://bitrix.telemart.ua/company/personal/user/0/tasks/task/view/{e.NavigationUrl}/";
            e.Handled = true;
        }
    }
}
