using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public record PriceEpsilonsDto
    {
        [JsonProperty("uah_price_epsilon")]
        public decimal UahPriceEpsilon { get; init; }

        [JsonProperty("usd_price_epsilon")]
        public decimal UsdPriceEpsilon { get; init; }

        [JsonProperty("eur_price_epsilon")]
        public decimal EurPriceEpsilon { get; init; }
    }
}