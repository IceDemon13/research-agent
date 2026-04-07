using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CanPayOrderViaCashRegistrar : CallEntityActionWithBodyRequestResultBase<OrderDto, FiscalCanPayDto>
    {
        public CanPayOrderViaCashRegistrar(int orderId, int cashboxId)
            : base(orderId, new FiscalCanPayDto(orderId, cashboxId), ApiResources.Orders, "can_pay")
        {
        }
    }

    public class FiscalCanPayDto
    {
        public FiscalCanPayDto(int orderId, int cashboxId)
        {
            OrderId = orderId;
            CashboxId = cashboxId;
        }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; init; }
    }
}