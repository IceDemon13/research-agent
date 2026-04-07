using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Template
{
    public class QueryContractorTemplate : QueryEntityRequestBase<ContractorTemplateDto>
    {
        public QueryContractorTemplate(int id)
            : base("templates", id)
        {
        }
    }
}