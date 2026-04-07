using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductContractorPricesDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("suppliers")]
        public List<ProductInfoContractorPriceDto> Suppliers { get; set; }

        [JsonProperty("competitors")]
        public List<ProductInfoContractorPriceDto> Competitors { get; set; }
    }
}