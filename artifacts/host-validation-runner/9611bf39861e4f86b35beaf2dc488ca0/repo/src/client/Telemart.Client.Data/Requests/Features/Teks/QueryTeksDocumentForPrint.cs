using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Teks;

namespace Telemart.Client.Data.Requests.Features.Teks
{
    public sealed class QueryTeksDocumentForPrint : QueryEntityRequestBase<TeksDocumentDto>
    {
        public QueryTeksDocumentForPrint(string ttn)
            : base(ApiResources.TeksDocuments, ttn, "document")
        {
        }
    }
}