using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductPriceConvertResponse
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }
    }
}