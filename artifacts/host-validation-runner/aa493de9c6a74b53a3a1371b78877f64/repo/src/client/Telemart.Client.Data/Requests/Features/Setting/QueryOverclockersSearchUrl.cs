using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryOverclockersSearchUrl : QueryEntityRequestBase<string>
    {
        public QueryOverclockersSearchUrl()
            : base(ApiResources.Settings, "overclockers_search_url")
        {
        }
    }
}