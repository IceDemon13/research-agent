using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class QueryUpDocument : QueryEntityRequestBase<UpDocumentDto>
    {
        public QueryUpDocument(string barcode)
            : base(ApiResources.UkrposhtaDocuments, barcode)
        {
        }
    }
}