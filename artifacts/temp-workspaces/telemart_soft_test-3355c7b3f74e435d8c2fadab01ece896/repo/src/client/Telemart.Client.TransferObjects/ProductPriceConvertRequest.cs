using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductPriceConvertRequest
    {
        public ProductPriceConvertRequest(int fromCurrencyId, int toCurrencyId, decimal value)
        {
            FromCurrencyId = fromCurrencyId;
            ToCurrencyId = toCurrencyId;
            Value = value;
        }

        [JsonProperty("from_currency_id")]
        public int FromCurrencyId { get; set; }

        [JsonProperty("to_currency_id")]
        public int ToCurrencyId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}