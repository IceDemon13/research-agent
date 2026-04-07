using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ReceiveGuestProductDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; init; }

        [JsonProperty("product")]
        public string Product { get; init; }

        [JsonProperty("keep_product")]
        public bool KeepProduct { get; init; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("additional_service_warehouse_id")]
        public int AdditionalServiceWarehouseId { get; init; }

        [JsonProperty("last_guest_product")]
        public bool LastGuestProduct { get; init; }
    }
}