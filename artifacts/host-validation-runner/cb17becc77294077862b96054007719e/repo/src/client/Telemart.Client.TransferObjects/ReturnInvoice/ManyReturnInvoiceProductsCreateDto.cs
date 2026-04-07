using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public sealed class ManyReturnInvoiceProductsCreateDto
    {
        public ManyReturnInvoiceProductsCreateDto(int invoiceId, ReturnInvoiceProductCreateDto[] products)
        {
            InvoiceId = invoiceId;
            Products = products;
        }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("products")]
        public ReturnInvoiceProductCreateDto[] Products { get; set; }
    }
}