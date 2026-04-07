using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.TradeIn
{
    public partial class TradeInsView : UserControl
    {
        public TradeInsView()
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

        private void TableView_CustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_CustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}