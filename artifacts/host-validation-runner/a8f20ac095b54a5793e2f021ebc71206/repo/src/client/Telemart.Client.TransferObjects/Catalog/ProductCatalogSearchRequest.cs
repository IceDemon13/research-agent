using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public sealed class ProductCatalogSearchRequest
    {
        [JsonProperty("product_names")]
        public string[] ProductNames { get; set; }

        [JsonProperty("product_ids")]
        public int[] ProductIds { get; set; }
    }
}