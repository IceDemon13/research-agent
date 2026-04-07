using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.TradeIn
{
    public partial class TradeInCoefView : UserControl
    {
        public TradeInCoefView()
        {
            InitializeComponent();
        }

        private void DataViewBase_OnShowGridMenu(object sender, GridMenuEventArgs e)
        {
            if (e.MenuType == GridMenuType.Column)
            {
                e.Handled = true;
            }
        }
    }
}