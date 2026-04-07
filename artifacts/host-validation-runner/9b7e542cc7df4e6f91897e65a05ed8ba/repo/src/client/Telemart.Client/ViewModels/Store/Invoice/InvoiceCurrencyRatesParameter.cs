using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceCurrencyRatesParameter
    {
        public InvoiceCurrencyRatesParameter(int invoiceId, IReadOnlyCollection<InvoiceCurrencyRateDto> currencyRates)
        {
            InvoiceId = invoiceId;
            CurrencyRates = currencyRates;
        }

        public int InvoiceId { get; }

        public IReadOnlyCollection<InvoiceCurrencyRateDto> CurrencyRates { get; }
    }
}