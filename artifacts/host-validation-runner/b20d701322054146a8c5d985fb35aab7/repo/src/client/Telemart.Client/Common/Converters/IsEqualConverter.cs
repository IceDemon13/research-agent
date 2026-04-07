using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class IsEqualConverter : ValueConverterBase
    {
        public bool Inverse { get; set; }

        public bool ToVisibility { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value != null && value.Same(parameter);

            result = Inverse ? !result : result;

            if (ToVisibility)
            {
                return result ? Visibility.Visible : Visibility.Collapsed;
            }

            return result;
        }
    }
}
