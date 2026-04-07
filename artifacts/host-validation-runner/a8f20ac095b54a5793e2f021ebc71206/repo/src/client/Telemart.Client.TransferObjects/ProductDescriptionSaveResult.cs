using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDescriptionSaveResult
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}