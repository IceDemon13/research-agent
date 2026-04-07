using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public sealed class CalculatePricesRequest
    {
        public CalculatePricesRequest(bool manualMode, IReadOnlyCollection<ProductPriceDto> products)
        {
            ManualMode = manualMode;
            Products = products;
        }

        [DataMember(Order = 1)]
        [JsonProperty("manual_mode")]
        public bool ManualMode { get; init; }

        [DataMember(Order = 2)]
        [JsonProperty("products")]
        public IReadOnlyCollection<ProductPriceDto> Products { get; init; }
    }
}