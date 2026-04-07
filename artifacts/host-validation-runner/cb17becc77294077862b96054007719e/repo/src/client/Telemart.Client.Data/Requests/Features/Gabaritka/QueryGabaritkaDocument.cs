using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Gabaritka;

namespace Telemart.Client.Data.Requests.Features.Gabaritka
{
    public sealed class QueryGabaritkaDocument : QueryEntityRequestBase<GabaritkaDocumentDto>
    {
        public QueryGabaritkaDocument(object number)
            : base(ApiResources.GabaritkaDocuments, number)
        {
        }
    }
}