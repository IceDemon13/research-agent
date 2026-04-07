using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class CreateContractorWarehouse : CreateEntityRequestBase<SupplierWarehouseDto, SupplierWarehouseDto>
    {
        public CreateContractorWarehouse(int contractorId, SupplierWarehouseDto dto)
            : base(dto, ApiResources.Contractors, contractorId.ToString(), "warehouses")
        {
        }
    }
}