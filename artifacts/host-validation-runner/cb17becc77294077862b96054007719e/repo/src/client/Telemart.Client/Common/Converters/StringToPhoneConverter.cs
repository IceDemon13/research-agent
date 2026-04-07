using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    internal sealed class StringToPhoneConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string phone = value.ToString();

            string phoneFormated;

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length == 10)
            {
                phoneFormated = $"({phone.Substring(0, 3)}) {phone.Substring(3, 3)}-{phone.Substring(6, 2)}-{phone.Substring(8, 2)}";
            }
            else
            {
                phoneFormated = phone;
            }

            return phoneFormated;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
        }
    }
}