using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.RobotProperties
{
    public partial class RobotCategoryPropertyView
    {
        public RobotCategoryPropertyView()
        {
            InitializeComponent();
        }

        private void TableView_OnShowGridMenu(object sender, GridMenuEventArgs e)
        {
            if (e.MenuType == GridMenuType.Column)
            {
                foreach (BarItem barItem in e.Items)
                {
                    if (barItem.BarItemName is DefaultColumnMenuItemNames.GroupBox or DefaultColumnMenuItemNames.GroupColumn)
                    {
                        barItem.IsVisible = false;
                    }
                }
            }
        }
    }
}