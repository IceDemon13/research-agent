using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductSalesHistoryItemDto
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("total_quantity")]
        public int TotalQuantity { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("last_sale_date_time")]
        public DateTime? LastSaleDateTime { get; set; }

        [JsonProperty("last_sale")]
        public bool LastSale { get; set; }
    }
}