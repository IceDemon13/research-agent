using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record CategorySpentBudgetsDto
    {
        [JsonProperty("category_spent_budgets")]
        public IReadOnlyCollection<CategorySpentBudgetDto> CategorySpentBudgets { get; init; }
    }
}