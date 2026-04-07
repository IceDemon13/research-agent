using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AdditionalServiceProductConsumableDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; init; }

        [JsonProperty("additional_service_product_id")]
        public int AdditionalServiceProductId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; init; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; init; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("scanned")]
        public bool Scanned { get; init; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; init; }

        [JsonProperty("guest_product")]
        public GuestProductDto GuestProduct { get; set; }
    }
}