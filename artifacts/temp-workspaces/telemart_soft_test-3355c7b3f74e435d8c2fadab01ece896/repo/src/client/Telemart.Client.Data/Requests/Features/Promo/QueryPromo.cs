using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Promo;

namespace Telemart.Client.Data.Requests.Features.Promo
{
    public class QueryPromo : QueryEntityRequestBase<PromoDto>
    {
        public QueryPromo(int id)
            : base(ApiResources.Promos, id)
        {
        }
    }
}