using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Contact
{
    public sealed class CreateContractorContact : CreateEntityResultRequestBase<ContractorContactDto, ContractorContactSaveDto>
    {
        public CreateContractorContact(int contractorId, ContractorContactSaveDto dto)
            : base(dto, ApiResources.Contractors, contractorId.ToString(), "contacts")
        {
        }
    }
}