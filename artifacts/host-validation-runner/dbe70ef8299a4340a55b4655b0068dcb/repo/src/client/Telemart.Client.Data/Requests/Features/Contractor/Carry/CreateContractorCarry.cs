using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class CreateContractorCarry : CreateEntityResultRequestBase<SupplierCarryDto, SupplierCarrySaveDto>
    {
        public CreateContractorCarry(int contractorId, SupplierCarrySaveDto dto)
            : base(dto, ApiResources.Contractors, contractorId.ToString(), "carries")
        {
        }
    }
}