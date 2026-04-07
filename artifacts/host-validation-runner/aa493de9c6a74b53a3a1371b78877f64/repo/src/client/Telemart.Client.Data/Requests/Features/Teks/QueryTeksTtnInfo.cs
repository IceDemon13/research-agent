using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Teks;

namespace Telemart.Client.Data.Requests.Features.Teks
{
    public sealed class QueryTeksTtnInfo : QueryEntityRequestBase<TeksDocumentDto>
    {
        public QueryTeksTtnInfo(string ttn)
        : base(ApiResources.TeksDocuments, ttn, "document_info")
        {
        }
    }
}