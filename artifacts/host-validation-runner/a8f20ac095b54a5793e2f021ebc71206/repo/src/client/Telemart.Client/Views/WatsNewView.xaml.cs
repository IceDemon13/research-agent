using DevExpress.Xpf.Bars;

namespace Telemart.Client.Views
{
    /// <summary>
    /// Interaction logic for WatsNewView.xaml
    /// </summary>
    public partial class WatsNewView
    {
        public WatsNewView()
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