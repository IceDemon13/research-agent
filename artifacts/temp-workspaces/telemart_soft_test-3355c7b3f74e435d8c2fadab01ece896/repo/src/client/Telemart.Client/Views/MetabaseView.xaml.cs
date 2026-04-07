using System.Windows.Controls;
using DevExpress.Xpf.Bars;

namespace Telemart.Client.Views
{
    public partial class MetabaseView : UserControl
    {
        public MetabaseView()
        {
            InitializeComponent();
        }

        private void NextClick(object sender, ItemClickEventArgs e)
        {
            if (WebBrowser.CanGoForward)
            {
                WebBrowser.GoForward();
            }
        }

        private void PrevClick(object sender, ItemClickEventArgs e)
        {
            if (WebBrowser.CanGoBack)
            {
                WebBrowser.GoBack();
            }
        }
    }
}