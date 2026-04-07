using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class ReturnOrderPayment : CallEntityActionWithBodyRequestResultBase<OrderDto, ReturnOrderPayment.ReturnOrderPaymentDto>
    {
        public ReturnOrderPayment(int orderId, int paymentId, int newPaymentId, decimal amount)
            : base(orderId, new ReturnOrderPaymentDto(orderId, paymentId, newPaymentId, amount), ApiResources.Orders, "return_payment")
        {
        }

        public sealed class ReturnOrderPaymentDto
        {
            public ReturnOrderPaymentDto(int orderId, int paymentId, int newPaymentId, decimal amount)
            {
                OrderId = orderId;
                PaymentId = paymentId;
                NewPaymentId = newPaymentId;
                Amount = amount;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; init; }

            [JsonProperty("payment_id")]
            public int PaymentId { get; init; }

            [JsonProperty("new_payment_id")]
            public int NewPaymentId { get; init; }

            [JsonProperty("amount")]
            public decimal Amount { get; init; }
        }
    }
}