using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class LockAdditionalServiceProduct : LockRequestBase<AdditionalServiceProductDto>
    {
        public LockAdditionalServiceProduct(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AdditionalServicesProducts, id)
        {
        }
    }
}