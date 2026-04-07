using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class QueryInventory : QueryEntityRequestBase<InventoryDto>
    {
        public QueryInventory(int id)
            : base(ApiResources.Inventories, id)
        {
        }
    }
}