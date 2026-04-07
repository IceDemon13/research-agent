using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductSnLengthDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("craeted_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }
    }
}