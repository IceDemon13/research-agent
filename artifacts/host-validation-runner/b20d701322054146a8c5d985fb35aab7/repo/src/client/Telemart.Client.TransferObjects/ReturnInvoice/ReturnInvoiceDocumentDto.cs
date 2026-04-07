using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceDocumentDto : ReturnInvoiceDocumentSimpleDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}