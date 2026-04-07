using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class QueryInventoryPrint : QueryEntityRequestBase<InventoryPrintDto>
    {
        public QueryInventoryPrint(int id)
            : base(ApiResources.Inventories, id, "print")
        {
        }
    }
}