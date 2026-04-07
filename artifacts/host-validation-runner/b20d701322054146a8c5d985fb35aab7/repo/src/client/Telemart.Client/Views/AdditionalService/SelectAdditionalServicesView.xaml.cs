using System.Windows.Controls;
using Telemart.Client.Common;

namespace Telemart.Client.Views.AdditionalService
{
    /// <summary>
    /// Interaction logic for SelectAdditionalServicesView.xaml.
    /// </summary>
    public partial class SelectAdditionalServicesView : UserControl
    {
        public SelectAdditionalServicesView()
        {
            InitializeComponent();
        }

        private void Product_OnRequestNavigation(object sender, DevExpress.Xpf.Editors.HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"{Constants.ProductBaseUrl}{e.NavigationUrl}";
            e.Handled = true;
        }
    }
}