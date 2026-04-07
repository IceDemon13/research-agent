using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class GuidToStringConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Guid guid)
            {
                return guid.ToString("D");
            }

            return null;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string guidStr = value?.ToString();

            if (Guid.TryParseExact(guidStr, "D", out Guid guid))
            {
                return guid;
            }

            return null;
        }
    }
}
