using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Money.Receive
{
    /// <summary>
    /// Interaction logic for BankPaymentsConfirmView.xaml
    /// </summary>
    public partial class BankPaymentsConfirmView
    {
        public BankPaymentsConfirmView()
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
