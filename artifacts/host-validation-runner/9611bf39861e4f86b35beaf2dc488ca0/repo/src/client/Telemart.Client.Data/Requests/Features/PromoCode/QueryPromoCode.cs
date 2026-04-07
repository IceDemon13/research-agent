using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode
{
    public class QueryPromoCode : QueryEntityRequestBase<PromoCodeFullDto>
    {
        public QueryPromoCode(int id)
            : base(ApiResources.PromoCodes, "find")
        {
            UrlParameters = new (string, object)[] { ("Id", id) };
        }

        public QueryPromoCode(string value)
            : base(ApiResources.PromoCodes, "find")
        {
            UrlParameters = new (string, object)[] { ("Value", value) };
        }
    }
}