using System.Windows.Controls;
using Telemart.Client.Common;

namespace Telemart.Client.Views.AssembledComputerRule
{
    public partial class SelectAssembledComputerRuleView
    {
        public SelectAssembledComputerRuleView()
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