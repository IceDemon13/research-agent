using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class UpdateContractorCarry : UpdateEntityResultRequestBase<SupplierCarryDto, SupplierCarrySaveDto>
    {
        public UpdateContractorCarry(int contractorId, SupplierCarrySaveDto dto)
            : base(dto, ApiResources.Contractors, contractorId, "carries", dto.Id)
        {
        }
    }
}