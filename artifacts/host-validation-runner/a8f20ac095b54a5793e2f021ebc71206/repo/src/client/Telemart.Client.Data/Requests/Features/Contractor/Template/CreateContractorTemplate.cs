using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public sealed class CreateContractorTemplate : CreateEntityRequestBase<ContractorTemplateDto, ContractorTemplateDto>
    {
        public CreateContractorTemplate(ContractorTemplateDto dto)
            : base(dto, ApiResources.ContractorTemplates)
        {
        }
    }
}
