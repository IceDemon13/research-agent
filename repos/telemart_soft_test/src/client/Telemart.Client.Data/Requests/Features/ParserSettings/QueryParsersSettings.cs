using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings
{
    public sealed class QueryParsersSettings : QueryEntitiesRequestBase<ParserSettingsDto>
    {
        public QueryParsersSettings()
            : base(ApiResources.ParserSettings)
        {
        }
    }
}
