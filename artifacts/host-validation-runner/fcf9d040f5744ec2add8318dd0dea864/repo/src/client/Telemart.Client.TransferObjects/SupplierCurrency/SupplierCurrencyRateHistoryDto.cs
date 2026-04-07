using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierCurrency
{
    public sealed class SupplierCurrencyRateHistoryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("rate")]
        public decimal Rate { get; set; }

        [JsonProperty("devotion")]
        public decimal Deviation { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}