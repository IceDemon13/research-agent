using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class SplitOrderFolder : CallEntityActionWithBodyRequestResultBase<OrderDto, SplitOrderFolder.SplitOrderFolderDto>
    {
        public SplitOrderFolder(int id, int orderFolderId, int quantity)
            : base(id, new SplitOrderFolderDto(id, orderFolderId, quantity), ApiResources.Orders, "split_folder")
        {
        }

        public class SplitOrderFolderDto
        {
            public SplitOrderFolderDto(int id, int orderFolderId, int quantity)
            {
                Id = id;
                OrderFolderId = orderFolderId;
                Quantity = quantity;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("order_folder_id")]
            public int OrderFolderId { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }
        }
    }
}
