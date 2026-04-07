using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public sealed class UpdateContractorTemplate : UpdateEntityRequestBase<ContractorTemplateDto, ContractorTemplateDto>
    {
        public UpdateContractorTemplate(ContractorTemplateDto dto)
            : base(dto, ApiResources.ContractorTemplates, dto.Id)
        {
        }
    }
}
