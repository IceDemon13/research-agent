using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record SaveInvoiceCategoryBudgetsDto
    {
        public SaveInvoiceCategoryBudgetsDto(IReadOnlyCollection<InvoiceCategoryBudgetDto> invoiceCategoryBudgets)
        {
            InvoiceCategoryBudgets = invoiceCategoryBudgets;
        }

        [JsonProperty("invoice_category_budgets")]
        public IReadOnlyCollection<InvoiceCategoryBudgetDto> InvoiceCategoryBudgets { get; init; }
    }
}