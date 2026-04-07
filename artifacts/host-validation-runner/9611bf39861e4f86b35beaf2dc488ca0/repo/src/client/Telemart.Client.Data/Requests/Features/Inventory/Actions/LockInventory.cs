using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory.Actions
{
    public sealed class LockInventory : LockRequestBase<InventoryDto>
    {
        public LockInventory(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Inventories, id)
        {
        }
    }
}