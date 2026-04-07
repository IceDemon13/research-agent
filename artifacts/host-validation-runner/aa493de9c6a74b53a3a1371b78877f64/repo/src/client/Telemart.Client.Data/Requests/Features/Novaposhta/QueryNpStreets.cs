using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpStreets : QueryEntitiesRequestBase<StreetDto>
    {
        public QueryNpStreets(int cityId)
            : base(ApiResources.Novaposhta, "streets")
        {
            UrlParameters = new (string, object)[] { ("cityId", cityId) };
        }
    }
}