using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common.Converters
{
    public class BooleanToEntityActiveConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool active)
            {
                return active
                    ? EntityActiveValue.ActiveValue
                    : EntityActiveValue.NoActive;
            }

            return null;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EntityActiveValue active)
            {
                return active == EntityActiveValue.ActiveValue;
            }

            return null;
        }
    }
}
