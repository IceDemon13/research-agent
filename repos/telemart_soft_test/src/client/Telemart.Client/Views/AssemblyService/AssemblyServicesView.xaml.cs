using System.Windows.Controls;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.AssemblyService
{
    /// <summary>
    /// Interaction logic for AssemblyServicesView.xaml
    /// </summary>
    public partial class AssemblyServicesView : UserControl
    {
        public AssemblyServicesView()
        {
            InitializeComponent();
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_OnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}