using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("sender_np_contractor_ref")]
        public string SenderNpContractorRef { get; set; }

        [JsonProperty("products")]
        public List<ReturnInvoiceProductSaveDto> Products { get; set; }
    }
}