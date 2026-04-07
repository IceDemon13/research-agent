using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public sealed class SetDefaultContractorTemplate : UpdateEntityRequestBase<ContractorTemplateDto, bool>
    {
        public SetDefaultContractorTemplate(int contractorTemplateId, bool isDefault)
            : base(isDefault, ApiResources.ContractorTemplates, contractorTemplateId, "default")
        {
        }
    }
}