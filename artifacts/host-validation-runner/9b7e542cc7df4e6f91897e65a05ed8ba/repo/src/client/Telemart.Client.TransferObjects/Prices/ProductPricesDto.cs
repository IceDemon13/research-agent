using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class ProductPricesDto
    {
        public ProductPricesDto(IReadOnlyCollection<ProductPriceDto> productPrices)
        {
            ProductPrices = productPrices;
        }

        public ProductPricesDto()
        {
        }

        [DataMember(Order = 1)]
        [JsonProperty("product_prices")]
        public IReadOnlyCollection<ProductPriceDto> ProductPrices { get; set; }
    }
}