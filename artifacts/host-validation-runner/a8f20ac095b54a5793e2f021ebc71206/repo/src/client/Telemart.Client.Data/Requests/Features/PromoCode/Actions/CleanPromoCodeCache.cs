using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.PromoCode.Actions
{
    public class CleanPromoCodeCache : CallEntityActionRequestBase<Result>
    {
        public CleanPromoCodeCache(int id)
            : base(id, ApiResources.PromoCodes, "clean_cache")
        {
        }
    }
}
