using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderPaymentResultDto
    {
        [JsonProperty("order_payment")]
        public OrderPaymentDto OrderPayment { get; set; }

        [JsonProperty("order")]
        public OrderDto Order { get; set; }
    }
}