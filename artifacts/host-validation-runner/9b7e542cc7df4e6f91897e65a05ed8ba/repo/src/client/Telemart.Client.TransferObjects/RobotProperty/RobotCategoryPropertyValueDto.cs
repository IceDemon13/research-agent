using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.RobotProperty
{
    public sealed class RobotCategoryPropertyValueDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("property_id")]
        public int PropertyId { get; init; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int? ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}