using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public sealed class IsEqualToVisibilityConverter : MultiValueConverterBase
    {
        public bool Inverse { get; set; }

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return null;
            }

            bool result = values[0].Same(values[1]);

            result = Inverse ? !result : result;

            return result
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
