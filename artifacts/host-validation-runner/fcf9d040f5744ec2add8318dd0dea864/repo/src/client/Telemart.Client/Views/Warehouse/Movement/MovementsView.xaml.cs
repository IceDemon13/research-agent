using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Warehouse.Movement
{
    /// <summary>
    /// Interaction logic for MovementsView.xaml
    /// </summary>
    public partial class MovementsView
    {
        public MovementsView()
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