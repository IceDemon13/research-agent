using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using DevExpress.Utils;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class BooleanAndMultiValueConverter : MultiValueConverterBase
    {
        public bool Inverse { get; set; }

        public bool ToVisibility { get; set; }

        public bool HiddenInsteadOfCollapsed { get; set; }

        public bool ToDefaultBoolean { get; set; }

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = ConvertInternal(values, Inverse);

            if (ToVisibility)
            {
                return result ? Visibility.Visible : HiddenInsteadOfCollapsed ? Visibility.Hidden : Visibility.Collapsed;
            }

            if (ToDefaultBoolean)
            {
                return result ? DefaultBoolean.True : DefaultBoolean.False;
            }

            return result;
        }

        private static bool ConvertInternal(object[] values, bool inverse)
        {
            bool result;

            if (values == null || values.Length == 0 || values.Any(x => x == null))
            {
                result = false;
            }
            else
            {
                result = true;

                foreach (object value in values)
                {
                    bool val = bool.TryParse(value.ToString(), out val) && val;
                    result = result & val;
                }
            }

            return inverse ? !result : result;
        }
    }
}