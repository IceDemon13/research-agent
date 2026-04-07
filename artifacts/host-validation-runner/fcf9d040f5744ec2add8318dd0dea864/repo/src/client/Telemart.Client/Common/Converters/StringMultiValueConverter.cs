using System;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class StringMultiValueConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.All(x => x == null || x is string) == true)
            {
                return values.Cast<string>().ToArray();
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }
}
