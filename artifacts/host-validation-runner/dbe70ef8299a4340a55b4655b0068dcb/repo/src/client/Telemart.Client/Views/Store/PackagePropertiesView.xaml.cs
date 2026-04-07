using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for PackagePropertiesView.xaml
    /// </summary>
    public partial class PackagePropertiesView
    {
        public PackagePropertiesView()
        {
            InitializeComponent();
        }

        private void TableViewOnCellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            TableView.PostEditor();
            GridControl.UpdateTotalSummary();
        }

        private void PlacesSpinEditOnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            GridControl.UpdateTotalSummary();
        }
    }
}
