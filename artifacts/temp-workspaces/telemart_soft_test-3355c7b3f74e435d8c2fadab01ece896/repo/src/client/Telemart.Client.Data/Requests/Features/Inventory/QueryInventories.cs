using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class QueryInventories : QueryEntitiesPagedRequestBase<InventoryDto>
    {
        public QueryInventories(IFilteringItem filter)
            : base(filter, ApiResources.Inventories)
        {
        }
    }
}