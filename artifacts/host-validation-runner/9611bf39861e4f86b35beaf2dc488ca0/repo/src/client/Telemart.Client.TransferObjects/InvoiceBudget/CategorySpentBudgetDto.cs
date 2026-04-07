using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record CategorySpentBudgetDto
    {
        [JsonProperty("category_id")]
        public int CategoryId { get; init; }
        
        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("invoice_budget")]
        public int InvoiceBudgetId { get; init; }

        [JsonProperty("spent_budget")]
        public decimal SpentBudget { get; init; }

        [JsonProperty("budget")]
        public decimal Budget { get; init; }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<int> ProductIds { get; init; }
    }
}