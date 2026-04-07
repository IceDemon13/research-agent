using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalDocument
{
    public class FiscalDocumentProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("document_id")]
        public int DocumentId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}