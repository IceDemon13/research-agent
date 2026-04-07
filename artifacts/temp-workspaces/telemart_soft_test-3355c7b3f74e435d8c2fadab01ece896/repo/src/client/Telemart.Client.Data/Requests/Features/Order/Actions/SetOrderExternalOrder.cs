using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class SetOrderExternalOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, SetOrderExternalOrder.SetOrderExternalOrderDto>
    {
        public SetOrderExternalOrder(int id, string externalOrderId)
            : base(id, new SetOrderExternalOrderDto(id, externalOrderId), ApiResources.Orders, "set_external_order")
        {
        }

        public class SetOrderExternalOrderDto
        {
            public SetOrderExternalOrderDto(int id, string externalOrderId)
            {
                Id = id;
                ExternalOrderId = externalOrderId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("external_order_id")]
            public string ExternalOrderId { get; init; }
        }
    }
}
