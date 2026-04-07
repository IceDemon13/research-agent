using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class StringToVisibilityConverter : ValueConverterBase
    {
        public bool Invert { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool collapsed = value == null || string.IsNullOrWhiteSpace(value.ToString());

            if (Invert)
            {
                collapsed = !collapsed;
            }

            Visibility visibility = collapsed
                ? Visibility.Collapsed
                : Visibility.Visible;

            return visibility;
        }
    }
}
