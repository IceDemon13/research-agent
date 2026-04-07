using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class CanEditOrderAdditionalService : CallEntityActionWithBodyRequestResultBase<OrderCanEditAdditionalServiceResponse, CanEditOrderAdditionalService.CanEditOrderAdditionalServiceDto>
    {
        public CanEditOrderAdditionalService(int id, int orderProductId)
            : base(id, new CanEditOrderAdditionalServiceDto(id, orderProductId), ApiResources.Orders, "can_edit_additional_service")
        {
        }

        public class CanEditOrderAdditionalServiceDto
        {
            public CanEditOrderAdditionalServiceDto(int id, int orderProductId)
            {
                Id = id;
                OrderProductId = orderProductId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("order_product_id")]
            public int OrderProductId { get; set; }
        }
    }
}
