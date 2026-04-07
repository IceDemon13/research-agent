using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Entity
{
    public class RefundRequestItem
    {
        public RefundRequestItem(string amount, string discount, string merchantId, string rrn)
        {
            Amount = amount;
            Discount = discount;
            MerchantId = merchantId;
            Rrn = rrn;
            SubMerchant = string.Empty;
        }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("discount")]
        public string Discount { get; set; }

        [JsonProperty("merchantId")]
        public string MerchantId { get; set; }

        [JsonProperty("rrn")]
        public string Rrn { get; set; }

        [JsonProperty("subMerchant")]
        public string SubMerchant { get; set; }
    }
}