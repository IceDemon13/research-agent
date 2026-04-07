using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public class DeleteParserSearchTemplateFeaturesRequest : DeleteEntityResultRequestBase<object>
    {
        public DeleteParserSearchTemplateFeaturesRequest(int id)
            : base(ApiResources.ParserSearchTemplates, "features", id)
        {
        }
    }
}