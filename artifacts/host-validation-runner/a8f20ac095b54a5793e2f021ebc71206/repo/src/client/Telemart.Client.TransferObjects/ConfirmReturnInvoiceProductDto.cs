using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ConfirmReturnInvoiceProductDto
    {
        public ConfirmReturnInvoiceProductDto(int returnInvoiceProductId, int confirmQuantity)
        {
            ReturnInvoiceProductId = returnInvoiceProductId;
            ConfirmQuantity = confirmQuantity;
        }

        [JsonProperty("return_invoice_product_id")]
        public int ReturnInvoiceProductId { get; set; }

        [JsonProperty("confirm_quantity")]
        public int ConfirmQuantity { get; set; }
    }
}