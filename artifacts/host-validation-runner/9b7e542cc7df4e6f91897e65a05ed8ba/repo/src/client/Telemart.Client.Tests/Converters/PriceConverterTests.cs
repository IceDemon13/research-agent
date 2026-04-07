using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Telemart.Client.Dictionaries;
using Telemart.Common.PriceConversion;
using Xunit;

namespace Telemart.Client.Tests.Converters
{
    public sealed class PriceConverterTests
    {
        [Theory]
        [InlineData(Currency.UahId, Currency.UsdId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdPlusId, CurrencyTypeIds.UsdPlusId, "4.0", "12345", "49380.0", false, 2)]
        [InlineData(Currency.UahId, Currency.UsdId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "0.0023657", "607.74", "1.437730518", false, 2)]
        [InlineData(Currency.UahId, Currency.UahId, CurrencyTypeIds.UahId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId, "605606", "60447.47412", "60447.47412", false, 2)]
        [InlineData(Currency.UsdId, Currency.UahId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "27.23456047", "1000.01", "27234.8328156047", false, 2)]
        [InlineData(Currency.UsdId, Currency.UahId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "35.707006", "808.60", "28872.6850516", false, 2)]
        [InlineData(Currency.EurId, Currency.UahId, CurrencyTypeIds.EurId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId, "100.0", "10", "1000.0", false, 2)]
        [InlineData(Currency.EurId, Currency.UahId, CurrencyTypeIds.EurId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId, "100.23", "708.6", "71023.0", true, 1)]
        [InlineData(Currency.EurId, Currency.UsdId, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "100.23", "708.6", "71023", true, 0)]
        [InlineData(Currency.EurId, Currency.UsdId, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "25.23", "1000.34", "25239", true, 0)]
        [InlineData(Currency.EurId, Currency.UsdId, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdMinusId, CurrencyTypeIds.UsdMinusId, "25.23", "1000.34", "25238.5782", false, 77)]
        [InlineData(Currency.EurId, Currency.UsdId, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdPlusId, CurrencyTypeIds.UsdPlusId, "25.54434498844666", "34.496", "881.17772472145598336", false, 0)]
        [InlineData(Currency.EurId, Currency.UahId, CurrencyTypeIds.EurId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdPlusId, "2", "0.25", "0", true, 0)]
        [InlineData(Currency.EurId, Currency.UsdId, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdPlusId, CurrencyTypeIds.UsdPlusId, "2", "3.25", "6", true, 0)]
        [InlineData(Currency.UahId, Currency.EurId, CurrencyTypeIds.EurId, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdPlusId, "2", "4", "8", true, 7)]
        public void PriceConverter_ShouldConvertTelemartPriceCorrect_Always(
            int fromCurrencyId,
            int toCurrencyId,
            int? fromCurrencyTypeId,
            int? toCurrencyTypeId,
            int usdCurrency,
            string rateStr,
            string priceToConvertStr,
            string expectedPriceStr,
            bool round,
            int roundDecimals)
        {
            decimal priceToConvert = Convert.ToDecimal(priceToConvertStr, System.Globalization.CultureInfo.InvariantCulture);
            decimal expectedPrice = Convert.ToDecimal(expectedPriceStr, System.Globalization.CultureInfo.InvariantCulture);
            decimal rate = Convert.ToDecimal(rateStr, System.Globalization.CultureInfo.InvariantCulture);

            ConversionRate[] telemartConversionRates =
            {
                new ConversionRate(fromCurrencyId, toCurrencyId, rate, roundDecimals, fromCurrencyTypeId, toCurrencyTypeId)
            };

            PriceConverter priceConverter = new PriceConverter(telemartConversionRates, PriceConverterMode.TelemartRate);

            decimal actualPrice = priceConverter.Convert(priceToConvert, fromCurrencyId, toCurrencyId, usdCurrency, round);

            Assert.Equal(expectedPrice, actualPrice);
        }

        [Fact]
        public void PriceConverter_Copy_ShouldReturnNewCopiedInstance_Always()
        {
            ConversionRate[] conversionRates =
            {
                new ConversionRate(1, 2, 34m, 3, 4, 5),
                new ConversionRate(2, 1, 34m, 3, 6, 2),
            };

            PriceConverter priceConverter = new PriceConverter(conversionRates, PriceConverterMode.TelemartRate);

            IPriceConverter copiedPriceConverter = priceConverter.Copy();

            Assert.NotEqual(priceConverter, copiedPriceConverter);
            Assert.Equal(priceConverter.GetRates().Count, copiedPriceConverter.GetRates().Count);
        }

        [Theory]
        [InlineData(3, 5, 3, 5, 8, 1, true, PriceConverterMode.InvoiceRate)]
        [InlineData(3, 5, 1, 2, 8, 1, false, PriceConverterMode.InvoiceRate)]
        [InlineData(3, 5, 4, 2, 8, 1, false, PriceConverterMode.SupplierRate)]
        [InlineData(3, 5, 3, 5, 8, 1, true, PriceConverterMode.SupplierRate)]
        [InlineData(3, 5, 4, 2, 8, 1, false, PriceConverterMode.TelemartRate)]
        [InlineData(3, 5, 3, 5, 1, 1, true, PriceConverterMode.TelemartRate)]
        [InlineData(Currency.UahId, Currency.UsdId, Currency.UahId, Currency.UsdId, 8, 1, false, PriceConverterMode.TelemartRate)]
        public void CanConvert_ShouldWorkCorrect_Always(
            int fromCurrencyId,
            int toCurrencyId,
            int fromCurrencyIdInConverter,
            int toCurrencyIdInConverter,
            int usdCurrency,
            int usdCurrencyInConverter,
            bool expectedResult,
            PriceConverterMode priceConverterMode)
        {
            ConversionRate[] conversionRates =
            {
                new ConversionRate(fromCurrencyIdInConverter, toCurrencyIdInConverter, 34m, 3, usdCurrencyInConverter, usdCurrencyInConverter)
            };

            PriceConverter priceConverter = new PriceConverter(conversionRates, priceConverterMode);

            Assert.Equal(expectedResult, priceConverter.CanConvert(fromCurrencyId, toCurrencyId, usdCurrency));
        }

        [Fact]
        public void PriceConverter_ShouldReturnMillionPrice_WhenRateNotFound()
        {
            ConversionRate[] conversionRates =
            {
                new ConversionRate(777, 999, 34.4m, 0, 1, 2)
            };

            PriceConverter priceConverter = new PriceConverter(conversionRates, PriceConverterMode.SupplierRate);

            decimal actualPrice = priceConverter.Convert(555, 123, 321, 12, true);

            Assert.Equal(1_000_000, actualPrice);
        }

        [Theory]
        [InlineData("34", "18")]
        [InlineData("34.56755", "1.002")]
        [InlineData("14.5858585854474334", "0.0012")]
        [InlineData("0.0", "12.3")]
        public void PriceConverter_ActualizeRatesMustWorkCorrect_Always(string previousRateStr, string newRateStr)
        {
            decimal newRate = Convert.ToDecimal(newRateStr, System.Globalization.CultureInfo.InvariantCulture);
            decimal previousRate = Convert.ToDecimal(previousRateStr, System.Globalization.CultureInfo.InvariantCulture);

            ConversionRate[] conversionRates =
            {
                new ConversionRate(Currency.UahId, Currency.UsdId, previousRate, 3, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId),
                new ConversionRate(Currency.UahId, Currency.UsdId, previousRate, 3, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdPlusId),
                new ConversionRate(Currency.EurId, Currency.UsdId, previousRate, 3, CurrencyTypeIds.EurId, CurrencyTypeIds.UsdPlusId)
            };

            PriceConverter priceConverter = new PriceConverter(conversionRates, PriceConverterMode.SupplierRate);

            ConversionRate[] newConversionRates =
            {
                new ConversionRate(Currency.UahId, Currency.UsdId, newRate, 3, CurrencyTypeIds.UahId, CurrencyTypeIds.UsdMinusId)
            };

            priceConverter.ActualizeRates(newConversionRates);

            Assert.Equal(2, priceConverter.GetRates().Count(x => x.Rate == newRate));
            Assert.Equal(1, priceConverter.GetRates().Count(x => x.Rate == previousRate));
            Assert.Equal(3, priceConverter.GetRates().Count);
        }
    }
}