using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryNpDocuments : QueryEntitiesPagedRequestBase<NpDocumentDto>
    {
        public QueryNpDocuments(IFilteringItem filter)
            : base(filter, ApiResources.NovaposhtaDocuments)
        {
        }
    }
}