using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record SaveInvoiceBudgetsDto
    {
        public SaveInvoiceBudgetsDto(IReadOnlyCollection<InvoiceBudgetDto> invoiceBudgets)
        {
            InvoiceBudgets = invoiceBudgets;
        }

        [JsonProperty("invoice_budgets")]
        public IReadOnlyCollection<InvoiceBudgetDto> InvoiceBudgets { get; init; }
    }
}