using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserContractorPriceDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("allow_documents")]
        public bool AllowDocuments { get; set; }
    }
}