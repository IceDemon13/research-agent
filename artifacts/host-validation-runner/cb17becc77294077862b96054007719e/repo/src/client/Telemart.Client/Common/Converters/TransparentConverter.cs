using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class TransparentConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
