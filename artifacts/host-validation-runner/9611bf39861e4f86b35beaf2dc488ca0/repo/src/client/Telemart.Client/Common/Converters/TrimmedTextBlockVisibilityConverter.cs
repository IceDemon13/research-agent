using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class TrimmedTextBlockVisibilityConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            FrameworkElement textBlock = (FrameworkElement)value;

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (((FrameworkElement)value).ActualWidth < ((FrameworkElement)value).DesiredSize.Width)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }
    }
}
