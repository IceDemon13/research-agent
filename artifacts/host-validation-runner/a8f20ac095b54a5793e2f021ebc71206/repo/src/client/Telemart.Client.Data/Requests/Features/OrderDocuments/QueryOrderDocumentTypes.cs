using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class QueryOrderDocumentTypes : QueryEntitiesRequestBase<OrderDocumentTypeDto>
    {
        public QueryOrderDocumentTypes()
            : base($"{ApiResources.Orders}/document_types")
        {
        }
    }
}