using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory.Action
{
    public class LockAccessory : LockRequestBase<AccessoryDto>
    {
        public LockAccessory(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Accessories, id)
        {
        }
    }
}