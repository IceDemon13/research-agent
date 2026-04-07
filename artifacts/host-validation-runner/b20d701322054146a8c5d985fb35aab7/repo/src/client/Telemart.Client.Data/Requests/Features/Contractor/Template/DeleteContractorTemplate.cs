using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public sealed class DeleteContractorTemplate : DeleteEntityRequestBase
    {
        public DeleteContractorTemplate(int entityId)
            : base(ApiResources.ContractorTemplates, entityId.ToString())
        {
        }
    }
}