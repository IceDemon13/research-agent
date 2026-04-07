using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public class QueryCategorySpentBudgetsDto
    {
        [JsonProperty("product_ids")]
        public int[] ProductIds { get; init; }
    }
}