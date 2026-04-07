using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility.Actions
{
    public class LockProductCompatibility : LockRequestBase<ProductCompatibilityDto>
    {
        public LockProductCompatibility(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ProductCompatibilities, id)
        {
        }
    }
}
