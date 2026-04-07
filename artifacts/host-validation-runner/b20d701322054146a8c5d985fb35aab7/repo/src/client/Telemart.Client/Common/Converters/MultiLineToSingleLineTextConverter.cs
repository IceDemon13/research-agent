using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class MultiLineToSingleLineTextConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value)?.Replace("\r\n", " ").Replace("\n", " ");
        }
    }
}
