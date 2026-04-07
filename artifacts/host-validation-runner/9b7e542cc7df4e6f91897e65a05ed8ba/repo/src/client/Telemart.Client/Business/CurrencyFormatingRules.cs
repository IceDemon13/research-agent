using System;
using System.Globalization;
using System.Linq;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business
{
    public static class CurrencyFormatingRules
    {
        static CurrencyFormatingRules()
        {
            EurFormat = "C2";
            UsdFormat = "C2";
            UahFormat = "C0";

            UsdCultureInfo = (CultureInfo)CultureInfo.DefaultThreadCurrentUICulture.Clone();
            UsdCultureInfo.NumberFormat.CurrencySymbol = "$";
            UsdCultureInfo.NumberFormat.CurrencyPositivePattern = 0;

            UahCultureInfo = (CultureInfo)CultureInfo.DefaultThreadCurrentUICulture.Clone();
            UahCultureInfo.NumberFormat.CurrencySymbol = string.Empty;
            UahCultureInfo.NumberFormat.CurrencyPositivePattern = 0;

            EurCultureInfo = (CultureInfo)CultureInfo.DefaultThreadCurrentUICulture.Clone();
            EurCultureInfo.NumberFormat.CurrencySymbol = "€";
            EurCultureInfo.NumberFormat.CurrencyPositivePattern = 1;
        }

        public static string EurFormat { get; }

        public static string UsdFormat { get; }

        public static string UahFormat { get; }

        public static CultureInfo UahCultureInfo { get; }

        public static CultureInfo UsdCultureInfo { get; }

        public static CultureInfo EurCultureInfo { get; }

        public static string ToUahStr(decimal price, string format = null, string currencySeparetor = " ")
        {
            return $"{price.ToString(format ?? UahFormat, UahCultureInfo)}{currencySeparetor}грн";
        }

        public static string ToUsdStr(decimal price, string format = null)
        {
            return price.ToString(format ?? UsdFormat, UsdCultureInfo);
        }

        public static string ToEurStr(decimal price, string format = null)
        {
            return price.ToString(format ?? EurFormat, EurCultureInfo);
        }

        public static string ToStr(decimal price, int currencyId, string format = null)
        {
            string res;

            if (currencyId == Currency.Uah.Id)
            {
                res = ToUahStr(price, format);
            }
            else if (currencyId == Currency.Usd.Id)
            {
                res = ToUsdStr(price, format);
            }
            else if (currencyId == Currency.Eur.Id)
            {
                res = ToEurStr(price, format);
            }
            else
            {
                throw new NotSupportedException();
            }

            return res;
        }

        public static string ToPricesString(Prices prices, string uahFormat = null, string usdFormat = null, string eurFormat = null)
        {
            string[] filledPrices = new[]
                {
                    prices.Usd > 0 ? ToUsdStr(prices.Usd, usdFormat) : null,
                    prices.Uah > 0 ? ToUahStr(prices.Uah, uahFormat) : null,
                    prices.Eur > 0 ? ToEurStr(prices.Eur, eurFormat) : null
                }
                .Where(x => x is not null)
                .ToArray();

            string priceFormatted = string.Join(", ", filledPrices);

            return string.IsNullOrWhiteSpace(priceFormatted) ? "0" : priceFormatted;
        }

        public static string ToCurrencyPriceString(Prices prices, int currencyId, string format = null)
        {
            string priceFormatted = currencyId switch
            {
                Currency.UahId => ToUahStr(prices.Uah, format),
                Currency.UsdId => ToUsdStr(prices.Usd, format),
                Currency.EurId => ToEurStr(prices.Eur, format),
                _ => "0"
            };

            return priceFormatted;
        }

        public static CultureInfo GetCultureInfoByCurrencyId(int currencyId)
        {
            CultureInfo cultureInfo = currencyId switch
            {
                Currency.UahId => UahCultureInfo,
                Currency.UsdId => UsdCultureInfo,
                Currency.EurId => EurCultureInfo,
                _ => throw new NotSupportedException()
            };

            return cultureInfo;
        }
    }
}