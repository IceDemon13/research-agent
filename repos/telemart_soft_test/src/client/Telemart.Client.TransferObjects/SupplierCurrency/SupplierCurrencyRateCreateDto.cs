using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierCurrency
{
    public sealed class SupplierCurrencyRateCreateDto
    {
        public SupplierCurrencyRateCreateDto(int? id, int supplierId, int currencyId, decimal rateNew)
        {
            Id = id;
            SupplierId = supplierId;
            CurrencyId = currencyId;
            RateNew = rateNew;
        }

        [JsonProperty("actual_currency_rate_id")]
        public int? Id { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("rate")]
        public decimal RateNew { get; set; }
    }
}