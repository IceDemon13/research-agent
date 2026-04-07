using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for StoreInvoicesView.xaml
    /// </summary>
    public partial class StoreInvoicesView
    {
        public StoreInvoicesView()
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
