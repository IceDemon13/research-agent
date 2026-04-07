using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode.Actions
{
    public class LockPromoCode : LockRequestBase<PromoCodeFullDto>
    {
        public LockPromoCode(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.PromoCodes, id)
        {
        }
    }
}
