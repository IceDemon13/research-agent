using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Settings.Printing
{
    /// <summary>
    /// Interaction logic for PrintingSettingsView.xaml
    /// </summary>
    public partial class PrintingSettingsView
    {
        public PrintingSettingsView()
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