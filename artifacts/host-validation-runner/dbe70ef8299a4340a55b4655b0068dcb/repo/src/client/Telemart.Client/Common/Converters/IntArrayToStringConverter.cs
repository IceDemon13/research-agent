using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class IntArrayToStringConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<int> enumerable = value as IEnumerable<int>;

            if (enumerable == null)
            {
                return null;
            }

            return string.Join(",", enumerable);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            int[] items = str.Split(',').Select(x => int.Parse(x)).ToArray();

            return items;
        }
    }
}
