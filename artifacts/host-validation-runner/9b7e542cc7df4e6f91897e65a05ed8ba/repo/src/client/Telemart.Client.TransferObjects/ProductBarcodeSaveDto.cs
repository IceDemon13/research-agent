using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductBarcodeSaveDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("barcode")]
        public string Barcode { get; init; }
    }
}