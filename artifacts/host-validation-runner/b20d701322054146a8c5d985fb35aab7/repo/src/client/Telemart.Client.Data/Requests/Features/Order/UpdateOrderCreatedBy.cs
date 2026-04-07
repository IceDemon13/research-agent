using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderCreatedBy : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderCreatedBy.UpdateOrderCreatedByDto>
    {
        public UpdateOrderCreatedBy(int orderId, int employeeId)
            : base(orderId, new UpdateOrderCreatedByDto { EmployeeId = employeeId }, ApiResources.Orders, "set_created_by")
        {
        }

        public sealed class UpdateOrderCreatedByDto
        {
            [JsonProperty("employee_id")]
            public int EmployeeId { get; init; }
        }
    }
}