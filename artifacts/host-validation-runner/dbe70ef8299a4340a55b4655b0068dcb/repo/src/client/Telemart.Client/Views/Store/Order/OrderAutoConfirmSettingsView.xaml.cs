using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store.Order
{
    public partial class OrderAutoConfirmSettingsView : UserControl
    {
        public OrderAutoConfirmSettingsView()
        {
            InitializeComponent();
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}