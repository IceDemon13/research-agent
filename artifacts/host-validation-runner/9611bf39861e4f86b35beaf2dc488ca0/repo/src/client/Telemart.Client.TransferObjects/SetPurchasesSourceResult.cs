using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class SetPurchasesSourceResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("purchase")]
        public PurchaseDto Purchase { get; set; }

        [JsonProperty("error_code_id")]
        public int? ErrorCodeId { get; set; }

        [JsonProperty("source_text")]
        public string SourceText { get; set; }
    }
}