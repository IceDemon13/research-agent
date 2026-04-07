using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Backlog
{
    /// <summary>
    /// Interaction logic for StoreOrdersView.xaml
    /// </summary>
    public partial class BacklogTasksView
    {
        public BacklogTasksView()
        {
            InitializeComponent();
        }

        private void BitrixId_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://bitrix.telemart.ua/company/personal/user/0/tasks/task/view/{e.NavigationUrl}/";
            e.Handled = true;
        }

        private void JiraId_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://telemart.atlassian.net/browse/{e.NavigationUrl}/";
            e.Handled = true;
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_OnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
