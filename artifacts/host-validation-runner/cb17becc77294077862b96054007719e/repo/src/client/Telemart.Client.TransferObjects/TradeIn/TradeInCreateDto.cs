using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCreateDto
    {
        [JsonProperty("customer_id")]
        public int? CustomerId { get; init; }

        [JsonProperty("order_id")]
        public int? OrderId { get; init; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; init; }

        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }

        [JsonProperty("first_name")]
        public string FirstName { get; init; }

        [JsonProperty("last_name")]
        public string LastName { get; init; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; init; }

        [JsonProperty("category_id")]
        public int? CategoryId { get; init; }

        [JsonProperty("brand")]
        public string Brand { get; init; }

        [JsonProperty("class_id")]
        public int ClassId { get; init; }

        [JsonProperty("warranty_id")]
        public int WarrantyId { get; init; }

        [JsonProperty("pack_id")]
        public int PackId { get; init; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; init; }

        [JsonProperty("model_or_pn")]
        public string ModelOrPn { get; init; }

        [JsonProperty("trade_in_segment_id")]
        public int? TradeInSegmentId { get; init; }

        [JsonProperty("customer_description")]
        public string CustomerDescription { get; init; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; init; }

        [JsonProperty("city_id")]
        public int? CityId { get; init; }

        [JsonProperty("delivery_data_out")]
        public DeliveryDataDto DeliveryDataOut { get; init; }

        [JsonProperty("inn")]
        public string Inn { get; init; }
    }
}