using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class UpdateContractor : UpdateEntityResultRequestBase<ContractorDto, ContractorSaveDto>
    {
        public UpdateContractor(ContractorSaveDto dto)
            : base(dto, ApiResources.Contractors, dto.Id)
        {
        }
    }
}
