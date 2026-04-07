using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductCatalogSaveResultDto
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("data")]
        public ProductCatalogDto Product { get; set; }
    }
}