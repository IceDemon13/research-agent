using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementAssemblyServiceProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("assembly_service_id")]
        public int AssemblyServiceId { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("assembly_service_product_id")]
        public int AssemblyServiceProductId { get; init; }

        [JsonProperty("scanned_out")]
        public bool ScannedOut { get; init; }

        [JsonProperty("scanned_in")]
        public bool ScannedIn { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }
    }
}