using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Data.Requests.Features.Accessory.Action
{
    public class UnlockAccessory : UnlockRequestBase<AccessoryDto>
    {
        public UnlockAccessory(int id, bool force = false)
            : base(force, ApiResources.Accessories, id)
        {
        }
    }
}