using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductsCatalogSaveResponse
    {
        [JsonProperty("results")]
        public GuidValuePair<ProductCatalogSaveResultDto>[] SaveResults { get; set; }
    }
}