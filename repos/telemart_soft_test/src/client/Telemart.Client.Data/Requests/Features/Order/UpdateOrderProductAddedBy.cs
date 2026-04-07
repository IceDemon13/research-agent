using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderProductAddedBy : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderProductAddedBy.UpdateOrderProductAddedByDto>
    {
        public UpdateOrderProductAddedBy(int orderId, int orderProductId, int employeeId)
            : base(orderId, new UpdateOrderProductAddedByDto { OrderProductId = orderProductId, EmployeeId = employeeId }, ApiResources.Orders, "set_product_added_by")
        {
        }

        public sealed class UpdateOrderProductAddedByDto
        {
            [JsonProperty("order_product_id")]
            public int OrderProductId { get; init; }

            [JsonProperty("employee_id")]
            public int EmployeeId { get; init; }
        }
    }
}