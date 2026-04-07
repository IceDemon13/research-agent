using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Hashtag.Actions
{
    public sealed class QueryHashtagsByPhone : QueryEntitiesRequestBase<HashtagDto>
    {
        public QueryHashtagsByPhone(string phone)
            : base(ApiResources.Customers, "actions", "get_hashtag_by_phone")
        {
            UrlParameters = new (string Name, object Value)[] { ("phone", phone) };
        }
    }
}