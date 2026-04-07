using System.Windows.Controls;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Parser.Dictionary
{
    public partial class CompareProductByFeaturesView
    {
        public CompareProductByFeaturesView()
        {
            InitializeComponent();
        }

        private void Product_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = e.NavigationUrl;
            e.Handled = true;
        }
    }
}