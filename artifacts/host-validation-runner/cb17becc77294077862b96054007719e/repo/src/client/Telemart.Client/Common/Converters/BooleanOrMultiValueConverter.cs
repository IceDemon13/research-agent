using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class BooleanOrMultiValueConverter : MultiValueConverterBase
    {
        public bool Inverse { get; set; }

        public bool ToVisibility { get; set; }

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            bool result = false;

            foreach (object value in values)
            {
                if (value is bool b && b)
                {
                    result = true;
                    break;
                }
            }

            result = Inverse
                ? !result
                : result;

            return ToVisibility
                ? (object)(result ? Visibility.Visible : Visibility.Collapsed)
                : result;
        }
    }
}