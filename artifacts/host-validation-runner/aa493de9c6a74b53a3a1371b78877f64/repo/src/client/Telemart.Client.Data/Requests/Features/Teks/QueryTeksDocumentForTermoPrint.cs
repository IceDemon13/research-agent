using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Teks;

namespace Telemart.Client.Data.Requests.Features.Teks
{
    public sealed class QueryTeksDocumentForTermoPrint : QueryEntityRequestBase<TeksDocumentDto>
    {
        public QueryTeksDocumentForTermoPrint(string ttn)
            : base(ApiResources.TeksDocuments, ttn, "termo_document")
        {
        }
    }
}