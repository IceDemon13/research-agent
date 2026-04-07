using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryOlxSearchUrl : QueryEntityRequestBase<string>
    {
        public QueryOlxSearchUrl()
        : base(ApiResources.Settings, "olx_search_url")
        {
        }
    }
}