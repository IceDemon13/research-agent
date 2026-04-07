using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class SetInvoiceCurrencyRate : CallEntityActionWithBodyRequestResultBase<InvoiceDto, SetInvoiceCurrencyRate.SetInvoiceCurrencyRateDto>
    {
        public SetInvoiceCurrencyRate(int id, IReadOnlyCollection<InvoiceCurrencyRateDto> currencyRates)
            : base(id, new SetInvoiceCurrencyRateDto(currencyRates), ApiResources.Invoices, "set_rate")
        {
        }

        public sealed class SetInvoiceCurrencyRateDto
        {
            public SetInvoiceCurrencyRateDto(IReadOnlyCollection<InvoiceCurrencyRateDto> currencyRates)
            {
                CurrencyRates = currencyRates;
            }

            [JsonProperty("currency_rates")]
            public IReadOnlyCollection<InvoiceCurrencyRateDto> CurrencyRates { get; set; }
        }
    }
}