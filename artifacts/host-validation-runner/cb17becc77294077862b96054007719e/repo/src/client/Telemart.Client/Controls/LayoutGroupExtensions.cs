using System.Collections;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.LayoutControl;

namespace Telemart.Client.Controls
{
    public class LayoutGroupExtensions
    {
        public static readonly DependencyProperty SelectedTabNameProperty = DependencyProperty.RegisterAttached(
            "SelectedTabName",
            typeof(string),
            typeof(LayoutGroupExtensions),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, SelectedTabNameChangedCallback));

        public static void SetSelectedTabName(DependencyObject element, string value)
        {
            element.SetValue(SelectedTabNameProperty, value);
        }

        public static string GetSelectedTabName(DependencyObject element)
        {
            return (string)element.GetValue(SelectedTabNameProperty);
        }

        private static void SelectedTabNameChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string tabName = e.NewValue?.ToString();

            if (!string.IsNullOrEmpty(tabName) && d is LayoutGroup layoutGroup && layoutGroup.Children is IEnumerable tabsEnumerable)
            {
                FrameworkElement tabToSelect = (FrameworkElement)tabsEnumerable
                    .Cast<object>()
                    .FirstOrDefault(x => x is FrameworkElement frameworkElement && frameworkElement.Name == tabName);

                layoutGroup.SelectTab(tabToSelect);
            }
        }
    }
}