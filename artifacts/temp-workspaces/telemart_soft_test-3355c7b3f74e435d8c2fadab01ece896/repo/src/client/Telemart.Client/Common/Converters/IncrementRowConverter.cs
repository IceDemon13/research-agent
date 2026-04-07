using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class IncrementRowConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intValue = (int)value;
            return ++intValue;
        }
    }
}
