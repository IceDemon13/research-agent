using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSearchTemplate;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public class QueryParserSearchTemplatesRequest : QueryEntitiesRequestBase<ParserSearchTemplateDto>
    {
        public QueryParserSearchTemplatesRequest()
            : base(ApiResources.ParserSearchTemplates)
        {
        }
    }
}