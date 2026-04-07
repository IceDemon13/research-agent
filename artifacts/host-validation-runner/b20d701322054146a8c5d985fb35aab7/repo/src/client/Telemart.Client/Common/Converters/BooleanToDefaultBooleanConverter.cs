using System;
using System.Globalization;
using DevExpress.Utils;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class BooleanToDefaultBooleanConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? DefaultBoolean.True : DefaultBoolean.False;
            }

            return DefaultBoolean.Default;
        }
    }
}
