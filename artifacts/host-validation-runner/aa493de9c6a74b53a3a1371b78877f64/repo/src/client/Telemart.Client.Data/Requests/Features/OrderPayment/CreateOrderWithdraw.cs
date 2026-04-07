using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderPayment
{
    public sealed class CreateOrderWithdraw : CallEntityActionWithBodyRequestResultBase<OrderWithdrawResultDto, CreateOrderWithdraw.OrderWithdrawCreateDto>
    {
        public CreateOrderWithdraw(
            int orderId,
            int? cashboxId,
            int? paymentId,
            int currencyId,
            decimal amount,
            string comment,
            bool autoRefund,
            RefundRequisitesDto requisites = null)
            : base(
                orderId,
                new OrderWithdrawCreateDto
                {
                    CashboxId = cashboxId,
                    PaymentId = paymentId,
                    CurrencyId = currencyId,
                    Amount = amount,
                    Comment = comment,
                    AutoRefund = autoRefund,
                    Requisites = requisites
                },
                ApiResources.Orders,
                "withdraw")
        {
        }

        public sealed class OrderWithdrawCreateDto
        {
            [JsonProperty("payment_id")]
            public int? PaymentId { get; init; }

            [JsonProperty("cashbox_id")]
            public int? CashboxId { get; init; }

            [JsonProperty("currency_id")]
            public int CurrencyId { get; init; }

            [JsonProperty("amount")]
            public decimal Amount { get; init; }

            [JsonProperty("comment")]
            public string Comment { get; init; }

            [JsonProperty("auto_refund")]
            public bool AutoRefund { get; init; }

            [JsonProperty("requisites")]
            public RefundRequisitesDto Requisites { get; init; }
        }
    }
}