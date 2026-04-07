using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Features.Currency;
using Telemart.Client.Data.Requests.Features.SupplierCurrency;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.SupplierCurrency;
using Telemart.Client.ViewModels.SupplierCurrency;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.Business.PriceConversion
{
    public class PriceConverterFactory : IPriceConverterFactory
    {
        private readonly IWebClient _webClient;

        public PriceConverterFactory(IWebClient webClient)
        {
            _webClient = webClient;
        }

        public async Task<IPriceConverter> CreateAsync()
        {
            IReadOnlyCollection<ConversionRate> conversionRates = await GetConversionRatesAsync();

            return new PriceConverter(conversionRates, PriceConverterMode.TelemartRate);
        }

        public async Task<IPriceConverter> CreateForSupplierAsync(int supplierId)
        {
            Task<IReadOnlyCollection<ConversionRate>> conversionRatesTask = GetConversionRatesAsync();
            Task<IReadOnlyCollection<ConversionRate>> supplierConversionRatesTask = GetSupplierConversionRatesAsync(supplierId);

            await Task.WhenAll(conversionRatesTask, supplierConversionRatesTask);

            IPriceConverter priceConverter = new PriceConverter(conversionRatesTask.Result, PriceConverterMode.SupplierRate);

            priceConverter.ActualizeRates(supplierConversionRatesTask.Result);

            return priceConverter;
        }

        public async Task<IReadOnlyDictionary<int, IPriceConverter>> CreateForInvoicesAsync(InvoiceDto[] invoices)
        {
            Dictionary<int, IPriceConverter> invoiceConverters = new Dictionary<int, IPriceConverter>();

            Task<IReadOnlyCollection<ConversionRate>> conversionRatesTask = GetConversionRatesAsync();

            Task<List<SupplierCurrencyRateActualDto>> actualSupplierRatesTask = _webClient
                .ExecuteApiRequestAsync(new QuerySupplierCurrencyRateActual(new SupplierCurrencyActualFilterItem(invoices.Select(x => x.SupplierId).Distinct().ToArray())));

            await Task.WhenAll(conversionRatesTask, actualSupplierRatesTask);

            IReadOnlyDictionary<int, ConversionRate[]> supplierConversionRates = actualSupplierRatesTask.Result
                .GroupBy(x => x.SupplierId)
                .ToDictionary(x => x.Key, x => x.Select(z => z.CreateConversionRate()).ToArray());

            foreach (InvoiceDto invoice in invoices)
            {
                ConversionRate[] invoiceSupplierRates = supplierConversionRates
                    .GetValueOrDefault(invoice.SupplierId, Array.Empty<ConversionRate>());

                ConversionRate[] invoiceConversionRates = invoice.CurrencyRates
                    .Select(x => x.CreateConversionRate())
                    .ToArray();

                IPriceConverter priceConverter = new PriceConverter(conversionRatesTask.Result, PriceConverterMode.InvoiceRate);

                priceConverter.ActualizeRates(invoiceSupplierRates);
                priceConverter.ActualizeRates(invoiceConversionRates);

                invoiceConverters[invoice.Id] = priceConverter;
            }

            return invoiceConverters;
        }

        public async Task<IPriceConverter> CreateForInvoiceAsync(int supplierId, IReadOnlyCollection<ConversionRate> invoiceConversionRates)
        {
            Task<IReadOnlyCollection<ConversionRate>> conversionRatesTask = GetConversionRatesAsync();
            Task<IReadOnlyCollection<ConversionRate>> supplierConversionRatesTask = GetSupplierConversionRatesAsync(supplierId);

            await Task.WhenAll(conversionRatesTask, supplierConversionRatesTask);

            IPriceConverter priceConverter = new PriceConverter(conversionRatesTask.Result, PriceConverterMode.InvoiceRate);

            priceConverter.ActualizeRates(supplierConversionRatesTask.Result);
            priceConverter.ActualizeRates(invoiceConversionRates);

            return priceConverter;
        }

        private async Task<IReadOnlyCollection<ConversionRate>> GetSupplierConversionRatesAsync(int supplierId)
        {
            List<SupplierCurrencyRateActualDto> actualSupplierRates = await _webClient
                .ExecuteApiRequestAsync(new QuerySupplierCurrencyRateActual(new SupplierCurrencyActualFilterItem(supplierId)));

            ConversionRate[] supplierConversionRates = actualSupplierRates
                .Select(x => x.CreateConversionRate())
                .ToArray();

            return supplierConversionRates;
        }

        private async Task<IReadOnlyCollection<ConversionRate>> GetConversionRatesAsync()
        {
            List<CurrencyTypeRateDto> currencyTypeRates = await _webClient.ExecuteApiRequestAsync(new QueryCurrencyTypeRates());

            ConversionRate[] conversionRates = currencyTypeRates
                .Select(x => new ConversionRate(
                    x.FromCurrencyId,
                    x.ToCurrencyId,
                    x.ConversionRate,
                    x.Digits,
                    x.FromCurrencyTypeId,
                    x.ToCurrencyTypeId))
                .ToArray();

            return conversionRates;
        }
    }
}