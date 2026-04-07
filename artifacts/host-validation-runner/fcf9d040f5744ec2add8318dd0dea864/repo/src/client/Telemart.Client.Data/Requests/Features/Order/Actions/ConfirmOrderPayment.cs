using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class ConfirmOrderPayment : CallEntityActionWithBodyRequestResultBase<OrderPaymentResultDto, ConfirmOrderPayment.OrderPaymentConfirmDto>
    {
        public ConfirmOrderPayment(int orderId, decimal amount, int paymentId)
            : base(orderId, new OrderPaymentConfirmDto(orderId, amount, paymentId), ApiResources.Orders, "confirm_payment")
        {
        }

        public class OrderPaymentConfirmDto
        {
            public OrderPaymentConfirmDto(int orderId, decimal amount, int paymentId)
            {
                OrderId = orderId;
                Amount = amount;
                PaymentId = paymentId;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; init; }

            [JsonProperty("payment_id")]
            public int PaymentId { get; init; }

            [JsonProperty("amount")]
            public decimal Amount { get; init; }
        }
    }
}