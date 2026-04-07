using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record OrderDocumentSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("ext")]
        public string Ext { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}