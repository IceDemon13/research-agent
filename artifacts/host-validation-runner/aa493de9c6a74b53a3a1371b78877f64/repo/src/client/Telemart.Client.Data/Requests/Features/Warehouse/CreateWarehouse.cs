using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class CreateWarehouse : CreateEntityResultRequestBase<WarehouseDto, WarehouseCreateDto>
    {
        public CreateWarehouse(WarehouseCreateDto dto)
            : base(dto, ApiResources.Warehouses)
        {
        }
    }
}