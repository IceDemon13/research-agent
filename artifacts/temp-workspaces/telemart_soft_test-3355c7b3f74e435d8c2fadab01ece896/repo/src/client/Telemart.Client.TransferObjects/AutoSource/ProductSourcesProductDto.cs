using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed record ProductSourcesProductDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("currency_out_id")]
        public int CurrencyOutId { get; init; }

        [JsonProperty("price_out")]
        public decimal PriceOut { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("parent_record_id")]
        public int? ParentRecordId { get; init; }

        [JsonProperty("is_additional_service")]
        public bool IsAdditionalService { get; set; }
    }
}