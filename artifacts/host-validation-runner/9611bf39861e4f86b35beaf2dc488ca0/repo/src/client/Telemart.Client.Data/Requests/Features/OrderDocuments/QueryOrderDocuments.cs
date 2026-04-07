using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class QueryOrderDocuments : QueryEntitiesRequestBase<OrderDocumentSimpleDto>
    {
        public QueryOrderDocuments(int orderId)
            : base($"{ApiResources.Orders}/{orderId}/documents")
        {
        }
    }
}