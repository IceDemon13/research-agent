using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class CanEditOrderAssembly : CallEntityActionWithBodyRequestResultBase<OrderCanEditAssemblyResponse, CanEditOrderAssembly.CanEditOrderAssemblyDto>
    {
        public CanEditOrderAssembly(int id, int orderFolderId)
            : base(id, new CanEditOrderAssemblyDto(id, orderFolderId), ApiResources.Orders, "can_edit_assembly")
        {
        }

        public class CanEditOrderAssemblyDto
        {
            public CanEditOrderAssemblyDto(int id, int orderFolderId)
            {
                Id = id;
                OrderFolderId = orderFolderId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("order_folder_id")]
            public int OrderFolderId { get; set; }
        }
    }
}
