using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record InvoiceCategoryBudgetDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("invoice_budget_id")]
        public int InvoiceBudgetId { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; init; }

        [JsonProperty("category_position")]
        public int CategoryPosition { get; init; }

        [JsonProperty("budget")]
        public decimal Budget { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}