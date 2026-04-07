using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class UpdateWarehouse : UpdateEntityResultRequestBase<WarehouseDto, WarehouseSaveDto>
    {
        public UpdateWarehouse(int id, WarehouseSaveDto dto)
            : base(dto, ApiResources.Warehouses, id)
        {
        }
    }
}