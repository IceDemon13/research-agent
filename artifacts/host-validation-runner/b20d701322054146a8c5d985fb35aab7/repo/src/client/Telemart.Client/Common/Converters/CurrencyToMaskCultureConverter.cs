using System;
using System.Globalization;
using Telemart.Client.Business;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common.Converters
{
    public sealed class CurrencyToMaskCultureConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? currencyId = (int?)value;

            return currencyId is null ? CurrencyFormatingRules.UahCultureInfo : CurrencyFormatingRules.GetCultureInfoByCurrencyId(currencyId.Value);
        }
    }
}