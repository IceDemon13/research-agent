using System.ComponentModel;
using System.Windows;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.AssembledComputerRule
{
    public partial class AssembledComputerRuleReserveView
    {
        public AssembledComputerRuleReserveView()
        {
            InitializeComponent();
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