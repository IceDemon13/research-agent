using System.Windows.Controls;
using DevExpress.Xpf.Bars;

namespace Telemart.Client.Views.Parser.Monitoring
{
    public partial class ParserCronicleView : UserControl
    {
        public ParserCronicleView()
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