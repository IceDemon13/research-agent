using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public sealed class ProductsCatalogSearchResponse
    {
        [JsonProperty("products")]
        public List<ProductCatalogDto> Products { get; set; }
    }
}