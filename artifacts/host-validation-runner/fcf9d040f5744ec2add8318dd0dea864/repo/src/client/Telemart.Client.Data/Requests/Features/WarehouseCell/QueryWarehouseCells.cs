using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class QueryWarehouseCells : QueryEntitiesRequestBase<WarehouseCellDto>
    {
        public QueryWarehouseCells(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Cells)
        {
        }

        public QueryWarehouseCells(WarehouseCellFilteringItem filteringItem)
            : base(filteringItem, ApiResources.Warehouses, filteringItem.WarehouseId, ApiResources.Cells)
        {
        }
    }
}