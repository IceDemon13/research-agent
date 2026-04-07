using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class SetAsMainContractorCarry : CallEntityActionRequestResultBase<SupplierCarryDto>
    {
        public SetAsMainContractorCarry(int contractorId, int supplierWarehouseId)
            : base(supplierWarehouseId, $"{ApiResources.Contractors}/{contractorId}/carries", "main")
        {
        }
    }
}