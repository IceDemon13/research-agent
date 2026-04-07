using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Showcase
{
    public partial class ShowcaseCategoryView : UserControl
    {
        public ShowcaseCategoryView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}