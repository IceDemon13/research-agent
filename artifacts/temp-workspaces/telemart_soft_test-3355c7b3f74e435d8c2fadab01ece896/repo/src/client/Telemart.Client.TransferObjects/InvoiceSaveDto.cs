using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("invoice_products")]
        public List<InvoiceProductSaveDto> InvoiceProducts { get; set; }
    }
}