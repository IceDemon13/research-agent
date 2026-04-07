using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Telemart.Client.Common.Converters
{
    public class OkCancelVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageBoxButton button && parameter is string part)
            {
                return (button == MessageBoxButton.OK && part == "OK") ||
                       (button == MessageBoxButton.OKCancel && (part == "OK" || part == "Cancel")) ||
                       (button == MessageBoxButton.YesNoCancel && part == "Cancel")
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
