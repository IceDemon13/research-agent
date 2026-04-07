using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode
{
    public class QueryPromoCodes : QueryEntitiesRequestBase<PromoCodeDto>
    {
        public QueryPromoCodes(IFilteringItem filter)
            : base(filter, ApiResources.PromoCodes)
        {
        }
    }
}
