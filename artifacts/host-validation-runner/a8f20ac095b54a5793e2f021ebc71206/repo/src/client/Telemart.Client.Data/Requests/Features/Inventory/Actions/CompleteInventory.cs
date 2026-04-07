using System.Net.Http;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory.Actions
{
    public sealed class CompleteInventory : CallEntityActionRequestResultBase<InventoryDto>
    {
        public CompleteInventory(int id)
            : base(id, ApiResources.Inventories, "complete")
        {
            Method = HttpMethod.Put;
        }
    }
}