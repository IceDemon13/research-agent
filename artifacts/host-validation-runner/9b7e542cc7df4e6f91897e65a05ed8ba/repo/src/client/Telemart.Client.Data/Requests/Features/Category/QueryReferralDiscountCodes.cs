using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class QueryReferralDiscountCodes : QueryEntitiesRequestBase<ReferralDiscountCodeDto>
    {
        public QueryReferralDiscountCodes()
            : base($"{ApiResources.Categories}/referral_discount_code")
        {
        }
    }
}