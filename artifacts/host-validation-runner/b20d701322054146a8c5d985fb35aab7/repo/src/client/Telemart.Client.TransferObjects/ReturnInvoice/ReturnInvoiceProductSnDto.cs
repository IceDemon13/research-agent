using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceProductSnDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("invoice_return_product_id")]
        public int InvoiceReturnProductId { get; set; }

        [JsonProperty("sn")]
        public string Sn { get; set; }
    }
}