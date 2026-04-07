using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public sealed class SavePricesRequest
    {
        public SavePricesRequest(IReadOnlyCollection<ProductPriceSaveDto> pricesToSave)
        {
            PricesToSave = pricesToSave;
        }

        public SavePricesRequest()
        {
        }

        [DataMember(Order = 1)]
        [JsonProperty("prices_to_save")]
        public IReadOnlyCollection<ProductPriceSaveDto> PricesToSave { get; }
    }
}