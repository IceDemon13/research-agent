using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.AutoSource
{
    public sealed class QueryAutoSourcesSettings : QueryEntitiesRequestBase<AutoSourceSettingDto>
    {
        public QueryAutoSourcesSettings()
        : base(ApiResources.AutoSource)
        {
        }
    }
}