namespace Telemart.Client.Views.AdditionalServiceProduct
{
    public partial class AdditionalServiceProductsView
    {
        public AdditionalServiceProductsView()
        {
            InitializeComponent();
        }

        private void TableView_CustomRowAppearance(object sender, DevExpress.Xpf.Grid.CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_CustomCellAppearance(object sender, DevExpress.Xpf.Grid.CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}