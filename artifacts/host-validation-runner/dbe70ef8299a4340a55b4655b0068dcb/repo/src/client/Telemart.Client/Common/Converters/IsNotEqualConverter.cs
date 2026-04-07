using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class IsNotEqualConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            return !value.Same(parameter);
        }
    }
}
