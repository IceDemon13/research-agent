using Telemart.Client.Common;

namespace Telemart.Client.Views.AssembledComputerRule
{
    /// <summary>
    /// Interaction logic for AssembledComputerRulesView.xaml.
    /// </summary>
    public partial class AssembledComputerRulesView
    {
        public AssembledComputerRulesView()
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