using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("default")]
        public bool Default { get; init; }

        [JsonProperty("price")]
        public decimal? Price { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("employee_name")]
        public string EmployeeName { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("created_by_name")]
        public string CreatedByName { get; init; }

        [JsonProperty("modified_by_name")]
        public string ModifiedByName { get; init; }

        [JsonProperty("trade_in_category_features")]
        public IReadOnlyCollection<TradeInSegmentCategoryFeatureDto> TradeInSegmentCategoryFeatures { get; init; }
    }
}
