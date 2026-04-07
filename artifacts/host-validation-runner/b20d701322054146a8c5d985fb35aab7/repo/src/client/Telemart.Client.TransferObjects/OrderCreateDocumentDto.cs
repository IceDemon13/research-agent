using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderCreateDocumentDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("ext")]
        public string Ext { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }
    }
}