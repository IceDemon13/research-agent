using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class DeleteWarehouseCell : DeleteEntityResultRequestBase<object>
    {
        public DeleteWarehouseCell(int warehouseId, int id)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Cells, id)
        {
        }
    }
}