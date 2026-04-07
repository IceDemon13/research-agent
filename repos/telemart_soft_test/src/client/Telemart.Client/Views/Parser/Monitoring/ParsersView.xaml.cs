using System.Diagnostics;
using System.Windows.Navigation;
using DevExpress.Xpf.Bars;

namespace Telemart.Client.Views.Parser.Monitoring
{
    public partial class ParsersView
    {
        public ParsersView()
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

        private void Refresh(object sender, ItemClickEventArgs e)
        {
            WebBrowser.Refresh(true);
        }

        private void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            Trace.WriteLine($"Navigating to {e.Uri}");
        }

        private void WebBrowserOnNavigated(object sender, NavigationEventArgs e)
        {
            Trace.WriteLine($"Navigated to {e.Uri}");
        }
    }
}
