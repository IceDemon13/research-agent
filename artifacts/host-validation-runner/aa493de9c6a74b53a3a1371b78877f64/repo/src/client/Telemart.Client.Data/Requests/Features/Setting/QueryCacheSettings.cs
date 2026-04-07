using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryCacheSettings : QueryEntityRequestBase<CacheSettingsDto>
    {
        public QueryCacheSettings()
            : base(ApiResources.Settings, "cache_settings")
        {
        }
    }
}