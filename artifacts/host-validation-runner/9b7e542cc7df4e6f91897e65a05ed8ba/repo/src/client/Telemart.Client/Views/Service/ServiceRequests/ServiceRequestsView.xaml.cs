using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Service.ServiceRequests
{
    /// <summary>
    /// Interaction logic for ServiceRequestsView.xaml
    /// </summary>
    public partial class ServiceRequestsView
    {
        public ServiceRequestsView()
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
