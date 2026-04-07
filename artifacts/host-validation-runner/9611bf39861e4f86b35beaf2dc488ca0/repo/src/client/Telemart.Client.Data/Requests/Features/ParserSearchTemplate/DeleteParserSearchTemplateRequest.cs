using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public class DeleteParserSearchTemplateRequest : DeleteEntityResultRequestBase<object>
    {
        public DeleteParserSearchTemplateRequest(int id)
            : base(ApiResources.ParserSearchTemplates, id)
        {
        }
    }
}