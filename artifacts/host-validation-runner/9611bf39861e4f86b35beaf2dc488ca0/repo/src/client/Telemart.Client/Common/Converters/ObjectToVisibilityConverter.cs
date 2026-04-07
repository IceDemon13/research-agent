using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class ObjectToVisibilityConverter : ValueConverterBase
    {
        public bool Negate { get; set; }

        public bool Collapse { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _ = bool.TryParse(parameter as string, out bool collapse);

            bool result = value != null;

            result = Negate
                ? !result
                : result;

            return result
                ? Visibility.Visible
                : (collapse || Collapse ? Visibility.Collapsed : Visibility.Hidden);
        }
    }
}
