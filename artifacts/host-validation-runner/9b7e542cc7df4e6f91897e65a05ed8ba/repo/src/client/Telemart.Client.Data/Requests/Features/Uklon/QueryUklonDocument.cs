using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Uklon
{
    public sealed class QueryUklonDocument : QueryEntityRequestBase<UklonDocumentDto>
    {
        public QueryUklonDocument(string id)
            : base(ApiResources.Uklon, "document", id)
        {
        }
    }
}