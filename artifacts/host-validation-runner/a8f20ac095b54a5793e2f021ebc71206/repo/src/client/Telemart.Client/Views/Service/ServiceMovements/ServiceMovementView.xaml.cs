using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Service.ServiceMovements
{
    /// <summary>
    /// Interaction logic for ServiceMovementView.xaml
    /// </summary>
    public partial class ServiceMovementView
    {
        public ServiceMovementView()
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
