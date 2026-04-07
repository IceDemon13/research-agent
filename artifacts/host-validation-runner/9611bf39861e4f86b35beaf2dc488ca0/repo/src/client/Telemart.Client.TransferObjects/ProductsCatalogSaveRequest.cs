using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductsCatalogSaveRequest
    {
        [JsonProperty("catalog")]
        public GuidValuePair<ProductCatalogSaveDto>[] Catalog { get; set; }
    }
}