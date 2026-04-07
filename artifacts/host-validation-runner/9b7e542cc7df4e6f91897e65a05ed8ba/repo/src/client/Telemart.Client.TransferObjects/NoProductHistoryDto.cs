using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class NoProductHistoryDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("no_product_reason_id")]
        public int? NoProductReasonId { get; set; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; set; }

        [JsonProperty("supplier_name")]
        public string SupplierName { get; set; }

        [JsonProperty("incorrect_price")]
        public decimal? IncorrectPrice { get; set; }

        [JsonProperty("correct_price")]
        public decimal? CorrectPrice { get; set; }
    }
}