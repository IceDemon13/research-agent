using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects
{
    public sealed class ComplectationGeneratorOptionsDto
    {
        [JsonProperty("use_transits")]
        public bool UseTransits { get; init; }

        [JsonProperty("use_purchases")]
        public bool UsePurchases { get; init; }

        [JsonProperty("use_warehouse")]
        public bool UseWarehouse { get; init; }
    }
}