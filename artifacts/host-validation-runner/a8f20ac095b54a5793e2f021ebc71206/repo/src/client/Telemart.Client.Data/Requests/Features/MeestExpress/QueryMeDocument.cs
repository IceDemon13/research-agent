using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress
{
    public class QueryMeDocument : QueryEntityRequestBase<MeDocumentDto>
    {
        public QueryMeDocument(object number)
            : base(ApiResources.MeestExpressDocuments, number)
        {
        }
    }
}