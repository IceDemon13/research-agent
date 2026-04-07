using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; init; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; init; }

        [JsonProperty("product_prefix_ru")]
        public string ProductPrefixRus { get; init; }

        [JsonProperty("product_prefix_ua")]
        public string ProductPrefixUa { get; init; }

        [JsonProperty("product_prefix_en")]
        public string ProductPrefixEn { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("quantity_out")]
        public int QuantityOut { get; init; }

        [JsonProperty("quantity_in")]
        public int QuantityIn { get; init; }

        [JsonProperty("price")]
        public decimal? Price { get; init; }

        [JsonProperty("usd_currency")]
        public int UsdCurrency { get; init; }

        [JsonProperty("created_by")]
        public int? CreatedBy { get; init; }

        [JsonProperty("weight")]
        public double Weight { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("serial_numbers")]
        public IReadOnlyCollection<MovementProductSnDto> SerialNumbers { get; init; }
    }
}