using System;
using System.Collections.Generic;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class StringsEnumerableToStringConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<string> enumerable = value as IEnumerable<string>;

            if (enumerable == null)
            {
                return null;
            }

            return string.Join(", ", enumerable);
        }
    }
}
