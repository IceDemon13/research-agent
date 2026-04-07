using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class QueryContractor : QueryEntityRequestBase<ContractorDto>
    {
        public QueryContractor(int id)
            : base(ApiResources.Contractors, id)
        {
        }
    }
}