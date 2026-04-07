using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class QueryOrderDocument : QueryEntityRequestBase<OrderDocumentDto>
    {
        public QueryOrderDocument(int id)
            : base($"{ApiResources.Orders}/documents/{id}")
        {
        }
    }
}