using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Service.ServiceRequests.Create
{
    /// <summary>
    /// Interaction logic for DeclarantPageView.xaml
    /// </summary>
    public partial class DeclarantPageView
    {
        public DeclarantPageView()
        {
            InitializeComponent();
        }

        private void ContractorsComboBoxOnPopupOpening(object sender, OpenPopupEventArgs e)
        {
            e.Cancel = ((ComboBoxEdit)sender).IsReadOnly;
        }
    }
}
