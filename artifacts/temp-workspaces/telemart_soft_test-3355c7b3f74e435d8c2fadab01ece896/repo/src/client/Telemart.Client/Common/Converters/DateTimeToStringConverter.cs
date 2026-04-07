using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    internal sealed class DateTimeToStringConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "?";
            }

            DateTime dateTime = (DateTime)value;

            return dateTime.ToString("g", CultureInfo.CurrentUICulture);
        }
    }
}