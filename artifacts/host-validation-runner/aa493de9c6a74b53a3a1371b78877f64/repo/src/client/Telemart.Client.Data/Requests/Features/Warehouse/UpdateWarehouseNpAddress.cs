using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class UpdateWarehouseNpAddress : UpdateEntityResultRequestBase<WarehouseDto, UpdateWarehouseNpAddressDto>
    {
        public UpdateWarehouseNpAddress(int warehouseId, UpdateWarehouseNpAddressDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, "update_np_address")
        {
        }
    }
}