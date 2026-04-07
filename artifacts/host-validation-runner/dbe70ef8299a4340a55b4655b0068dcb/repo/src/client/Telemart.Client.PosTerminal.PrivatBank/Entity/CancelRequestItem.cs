using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Entity
{
    public class CancelRequestItem
    {
        public CancelRequestItem(string invoiceNumber)
        {
            InvoiceNumber = invoiceNumber;
        }

        [JsonProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }
    }
}