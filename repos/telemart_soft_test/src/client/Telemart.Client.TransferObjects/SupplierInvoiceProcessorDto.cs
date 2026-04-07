using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SupplierInvoiceProcessorDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("allow_reserve")]
        public bool AllowReserve { get; set; }

        [JsonProperty("allow_purchase")]
        public bool AllowPurchase { get; set; }
    }
}