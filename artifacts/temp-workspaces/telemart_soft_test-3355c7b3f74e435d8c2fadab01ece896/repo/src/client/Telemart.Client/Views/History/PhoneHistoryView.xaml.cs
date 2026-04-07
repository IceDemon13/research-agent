using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.History
{
    /// <summary>
    /// Interaction logic for PhoneHistoryView.xaml
    /// </summary>
    public partial class PhoneHistoryView
    {
        public PhoneHistoryView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
