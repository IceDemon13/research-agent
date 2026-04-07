using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings
{
    public sealed class QueryParserSettings : QueryEntityRequestBase<ParserSettingsDto>
    {
        public QueryParserSettings(object id)
            : base(ApiResources.ParserSettings, id)
        {
        }
    }
}