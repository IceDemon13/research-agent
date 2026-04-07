using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Showcase
{
    public partial class AutoShowcasesView : UserControl
    {
        public AutoShowcasesView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableViewCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}