using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class OrderSetManager : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderSetManager.OrderManagerSaveDto>
    {
        public OrderSetManager(int id, int employeeId)
            : base(id, new OrderManagerSaveDto(id, employeeId), ApiResources.Orders, "set_manager")
        {
        }

        public class OrderManagerSaveDto
        {
            public OrderManagerSaveDto(int id, int employeeId)
            {
                Id = id;
                EmployeeId = employeeId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("employee_id")]
            public int EmployeeId { get; set; }
        }
    }
}
