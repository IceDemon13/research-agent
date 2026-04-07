using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateOrderPaymentDto
    {
        public UpdateOrderPaymentDto(int orderId, int paymentId)
        {
            Id = orderId;
            PaymentId = paymentId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }
    }
}