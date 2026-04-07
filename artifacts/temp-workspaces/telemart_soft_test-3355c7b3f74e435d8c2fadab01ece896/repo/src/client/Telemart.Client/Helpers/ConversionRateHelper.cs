using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Dictionaries;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.Helpers
{
    public static class ConversionRateHelper
    {
        public static IEnumerable<T> UnionWithInvertedRates<T>(this IEnumerable<T> conversionRates)
            where T : IConversionRate, new()
        {
            HashSet<(int fromCurrencyId, int toCurrencyId)> conversionRatesHashSet = conversionRates
                .Select(x => (x.FromCurrencyId, x.ToCurrencyId))
                .ToHashSet();

            foreach (T conversionRate in conversionRates)
            {
                bool revertedRateAdded = conversionRatesHashSet.Add((conversionRate.ToCurrencyId, conversionRate.FromCurrencyId));

                if (revertedRateAdded)
                {
                    T revertedConversionRate = new T()
                    {
                        Rate = 1 / conversionRate.Rate,
                        FromCurrencyId = conversionRate.ToCurrencyId,
                        ToCurrencyId = conversionRate.FromCurrencyId,
                        Decimals = Currency.GetById(conversionRate.ToCurrencyId).Decimals
                    };

                    yield return revertedConversionRate;
                }

                yield return conversionRate;
            }
        }
    }
}