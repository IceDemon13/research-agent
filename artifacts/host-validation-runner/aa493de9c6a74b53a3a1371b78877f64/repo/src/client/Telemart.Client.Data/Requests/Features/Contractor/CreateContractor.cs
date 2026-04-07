using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class CreateContractor : CreateEntityResultRequestBase<ContractorDto, ContractorCreateDto>
    {
        public CreateContractor(ContractorCreateDto dto)
            : base(dto, ApiResources.Contractors)
        {
        }
    }
}