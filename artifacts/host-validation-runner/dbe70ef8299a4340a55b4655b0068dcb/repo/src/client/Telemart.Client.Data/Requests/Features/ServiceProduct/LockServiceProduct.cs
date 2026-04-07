using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class LockServiceProduct : LockRequestBase<ServiceProductDto>
    {
        public LockServiceProduct(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceProducts, id)
        {
        }
    }
}
