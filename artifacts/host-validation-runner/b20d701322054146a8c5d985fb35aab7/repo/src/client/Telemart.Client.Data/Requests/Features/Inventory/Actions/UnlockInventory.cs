using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory.Actions
{
    public sealed class UnlockInventory : UnlockRequestBase<InventoryDto>
    {
        public UnlockInventory(int id, bool force = false)
            : base(force, ApiResources.Inventories, id)
        {
        }
    }
}
