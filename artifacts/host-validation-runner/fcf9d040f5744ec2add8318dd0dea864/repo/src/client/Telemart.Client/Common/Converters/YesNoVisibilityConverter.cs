using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Telemart.Client.Common.Converters
{
    public class YesNoVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageBoxButton button)
            {
                return button == MessageBoxButton.YesNo || button == MessageBoxButton.YesNoCancel
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
