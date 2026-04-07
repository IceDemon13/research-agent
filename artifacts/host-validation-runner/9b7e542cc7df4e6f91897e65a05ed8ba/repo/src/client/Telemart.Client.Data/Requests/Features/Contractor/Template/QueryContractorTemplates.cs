using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public sealed class QueryContractorTemplates : QueryEntitiesPagedRequestBase<ContractorTemplateDto>
    {
        public QueryContractorTemplates(int contractorId)
            : base(ApiResources.Contractors, contractorId, "templates")
        {
        }
    }
}