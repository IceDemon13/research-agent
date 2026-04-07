using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class SetAsNotMainContractorCarry : CallEntityActionRequestResultBase<SupplierCarryDto>
    {
        public SetAsNotMainContractorCarry(int contractorId, int supplierWarehouseId)
            : base(supplierWarehouseId, $"{ApiResources.Contractors}/{contractorId}/carries", "not-main")
        {
        }
    }
}