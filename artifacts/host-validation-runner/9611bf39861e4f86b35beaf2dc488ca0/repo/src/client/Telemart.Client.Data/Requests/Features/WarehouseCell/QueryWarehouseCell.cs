using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class QueryWarehouseCell : QueryEntityRequestBase<WarehouseCellDto>
    {
        public QueryWarehouseCell(int warehouseId, int cellId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Cells, cellId)
        {
        }
    }
}