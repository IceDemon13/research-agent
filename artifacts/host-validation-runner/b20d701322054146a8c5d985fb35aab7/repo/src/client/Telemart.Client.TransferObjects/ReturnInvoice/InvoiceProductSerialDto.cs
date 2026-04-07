using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class InvoiceProductSerialDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }
    }
}