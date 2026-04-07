using DevExpress.Xpf.Core;

namespace Telemart.Client.Views.Base
{
    public class MinimizedThemedWindow : ThemedWindow
    {
        public MinimizedThemedWindow()
        {
            Loaded += (s, e) => WindowState = System.Windows.WindowState.Minimized;
        }
    }
}