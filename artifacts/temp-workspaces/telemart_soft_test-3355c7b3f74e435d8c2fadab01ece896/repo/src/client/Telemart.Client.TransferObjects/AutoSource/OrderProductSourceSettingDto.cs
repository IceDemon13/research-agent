using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed record OrderProductSourceSettingDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("source_id")]
        public int SourceId { get; init; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; init; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; init; }

        [JsonProperty("warehouse_type_id")]
        public int? WarehouseTypeId { get; init; }

        [JsonProperty("priority")]
        public int Priority { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

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