using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductPurchaseHistoryItemDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [JsonProperty("date_close")]
        public DateTime DateClose { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("last_purchase")]
        public bool LastPurchase { get; set; }
    }
}