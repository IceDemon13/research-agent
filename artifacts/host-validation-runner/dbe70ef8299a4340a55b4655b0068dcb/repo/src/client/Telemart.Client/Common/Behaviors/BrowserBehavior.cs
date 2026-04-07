using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.Common.Behaviors
{
    internal static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser webBrowser)
        {
            return (string)webBrowser.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser webBrowser, string value)
        {
            webBrowser.SetValue(HtmlProperty, value);
        }

        private static void OnHtmlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            WebBrowser webBrowser = dependencyObject as WebBrowser;
            webBrowser?.NavigateToString((string)args.NewValue);
        }
    }
}