using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Entity
{
    public class PurchaseRequestItem
    {
        public PurchaseRequestItem(string amount, string discount, string merchantId)
        {
            Amount = amount;
            Discount = discount;
            MerchantId = merchantId;
        }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("discount")]
        public string Discount { get; set; }

        [JsonProperty("merchantId")]
        public string MerchantId { get; set; }
    }
}
