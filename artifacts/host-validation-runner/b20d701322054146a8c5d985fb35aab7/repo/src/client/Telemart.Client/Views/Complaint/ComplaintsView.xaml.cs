using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Complaint
{
    /// <summary>
    /// Interaction logic for ComplaintsView.xaml
    /// </summary>
    public partial class ComplaintsView
    {
        public ComplaintsView()
        {
            InitializeComponent();
        }

        private void BitrixId_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NavigationUrl))
            {
                e.Cancel = true;
            }
            else
            {
                e.NavigationUrl = $"https://bitrix.telemart.ua/company/personal/user/0/tasks/task/view/{e.NavigationUrl}/";
            }

            e.Handled = true;
        }
    }
}
