using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementAdditionalServiceProductDto
    {
        [JsonProperty("parent_order_product_product_id")]
        public int? ParentOrderProductProductId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("additional_service_product_id")]
        public int AdditionalServiceProductId { get; init; }

        [JsonProperty("primary_additional_service_product_sn")]
        public string PrimaryAdditionalServiceProductSn { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("product_id_from_additional_service")]
        public int ProductIdFromAdditionalService { get; init; }

        [JsonProperty("additional_service_name")]
        public string AdditionalServiceName { get; init; }

        [JsonProperty("additional_service_name_ua")]
        public string AdditionalServiceNameUa { get; init; }

        [JsonProperty("additional_service_name_en")]
        public string AdditionalServiceNameEn { get; init; }

        [JsonProperty("scanned_out")]
        public bool ScannedOut { get; init; }

        [JsonProperty("scanned_in")]
        public bool ScannedIn { get; init; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("consumable_products")]
        public IReadOnlyCollection<AdditionalServiceProductConsumableDto> ConsumableProducts { get; init; }
    }
}