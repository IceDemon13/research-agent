using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using Telemart.Client.Common;

namespace Telemart.Client.Views.Customer
{
    /// <summary>
    /// Interaction logic for CustomerView.xaml
    /// </summary>
    public partial class CustomerView : UserControl
    {
        public CustomerView()
        {
            InitializeComponent();
        }

        private void Product_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"{Constants.BaseUrl}/assembly/configuration-{e.NavigationUrl}";
            e.Handled = true;
        }
    }
}