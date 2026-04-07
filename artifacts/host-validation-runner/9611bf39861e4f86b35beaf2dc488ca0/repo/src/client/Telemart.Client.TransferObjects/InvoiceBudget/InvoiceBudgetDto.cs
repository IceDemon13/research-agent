using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.InvoiceBudget
{
    public record InvoiceBudgetDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("budget")]
        public decimal Budget { get; init; }

        [JsonProperty("date_from")]
        public DateOnly DateFrom { get; init; }

        [JsonProperty("date_to")]
        public DateOnly DateTo { get; init; }

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