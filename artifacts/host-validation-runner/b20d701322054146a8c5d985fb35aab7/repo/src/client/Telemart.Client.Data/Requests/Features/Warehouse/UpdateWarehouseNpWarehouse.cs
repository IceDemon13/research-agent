using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class UpdateWarehouseNpWarehouse : UpdateEntityResultRequestBase<WarehouseDto, UpdateWarehouseNpWarehouseDto>
    {
        public UpdateWarehouseNpWarehouse(int warehouseId, UpdateWarehouseNpWarehouseDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, "update_np_warehouse")
        {
        }
    }
}