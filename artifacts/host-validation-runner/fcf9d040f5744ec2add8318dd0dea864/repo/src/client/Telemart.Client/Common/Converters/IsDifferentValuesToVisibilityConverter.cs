using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class IsDifferentValuesToVisibilityConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return null;
            }

            return values[0].Same(values[1]) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
