using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class UpdateContractorWarehouse : UpdateEntityRequestBase<SupplierWarehouseDto, SupplierWarehouseDto>
    {
        public UpdateContractorWarehouse(int contractorId, SupplierWarehouseDto dto)
            : base(dto, ApiResources.Contractors, contractorId, "warehouses", dto.Id)
        {
        }
    }
}