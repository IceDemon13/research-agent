using Newtonsoft.Json;
using Telemart.Client.PosTerminal.PrivatBank.Responses;

namespace Telemart.Client.PosTerminal.PrivatBank.Entity
{
    public class PurchaseResponseItem : ResponseItemBase
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("approvalCode")]
        public string ApprovalCode { get; set; }

        [JsonProperty("captureReference")]
        public string CaptureReference { get; set; }

        [JsonProperty("cardExpiryDate")]
        public string CardExpiryDate { get; set; }

        [JsonProperty("cardHolderName")]
        public string CardHolderName { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("discount")]
        public string Discount { get; set; }

        [JsonProperty("hstFld63Sf89")]
        public string HstFld63Sf89 { get; set; }

        [JsonProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [JsonProperty("issuerName")]
        public string IssuerName { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }

        [JsonProperty("pan")]
        public string Pan { get; set; }

        [JsonProperty("posConditionCode")]
        public string PosConditionCode { get; set; }

        [JsonProperty("posEntryMode")]
        public string PosEntryMode { get; set; }

        [JsonProperty("processingCode")]
        public string ProcessingCode { get; set; }

        [JsonProperty("receipt")]
        public string Receipt { get; set; }

        [JsonProperty("rrn")]
        public string Rrn { get; set; }

        [JsonProperty("terminalId")]
        public string TerminalId { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("track1")]
        public string Track1 { get; set; }

        [JsonProperty("track2")]
        public string Track2 { get; set; }
    }
}
