using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Contact
{
    public sealed class UpdateContractorContact : UpdateEntityResultRequestBase<ContractorContactDto, ContractorContactSaveDto>
    {
        public UpdateContractorContact(int contractorId, ContractorContactSaveDto dto)
            : base(dto, ApiResources.Contractors, contractorId, "contacts", dto.Id)
        {
        }
    }
}