using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderWithdrawResultDto
    {
        [JsonProperty("order_payment")]
        public OrderPaymentDto OrderPayment { get; set; }

        [JsonProperty("order")]
        public OrderDto Order { get; set; }

        [JsonProperty("money_refund")]
        public RefundDto MoneyRefund { get; set; }
    }
}