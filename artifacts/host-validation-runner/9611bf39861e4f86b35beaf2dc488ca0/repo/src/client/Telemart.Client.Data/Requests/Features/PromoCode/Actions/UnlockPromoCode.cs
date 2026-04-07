using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode.Actions
{
    public class UnlockPromoCode : UnlockRequestBase<PromoCodeFullDto>
    {
        public UnlockPromoCode(int id, bool force = false)
            : base(force, ApiResources.PromoCodes, id)
        {
        }
    }
}
