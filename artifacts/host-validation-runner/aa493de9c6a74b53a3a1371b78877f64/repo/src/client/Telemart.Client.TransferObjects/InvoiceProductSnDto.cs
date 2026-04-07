using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceProductSnDto
    {
        [JsonProperty("id_record")]
        public int Id { get; set; }

        [JsonProperty("invoice_product_id")]
        public int InvoiceProductId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }
    }
}