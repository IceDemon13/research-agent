using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class ChangeMoneyBackAmount : CallEntityActionWithBodyRequestResultBase<OrderDto, MoneyBackAmountDto>
    {
        public ChangeMoneyBackAmount(int orderId, decimal amount)
            : base(orderId, new MoneyBackAmountDto { OrderId = orderId, Amount = amount },  ApiResources.Orders, "change_money_back_amount")
        {
        }
    }

    public sealed record MoneyBackAmountDto
    {
        [JsonProperty("id")]
        public int OrderId { get; init; }

        [JsonProperty("amount")]
        public decimal Amount { get; init; }
    }
}