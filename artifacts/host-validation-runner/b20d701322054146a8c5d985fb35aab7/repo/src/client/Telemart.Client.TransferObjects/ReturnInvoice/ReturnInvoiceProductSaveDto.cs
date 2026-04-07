using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceProductSaveDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("out_quantity")]
        public int OutQuantity { get; set; }

        [JsonProperty("serial_numbers")]
        public List<string> SerialNumbers { get; set; }
    }
}