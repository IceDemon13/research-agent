using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class RepairInvoiceDto
    {
        [JsonProperty("service_invoice_product_id")]
        public int ServiceInvoiceProductId { get; set; }

        [JsonProperty("accepted")]
        public bool Accepted { get; set; }

        [JsonProperty("repair_invoice")]
        public string RepairInvoice { get; set; }
    }
}