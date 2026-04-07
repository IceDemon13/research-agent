using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class QueryContractorCarries : QueryEntitiesRequestBase<SupplierCarryDto>
    {
        public QueryContractorCarries(int contractorId)
            : base(ApiResources.Contractors, contractorId, "carries")
        {
        }
    }
}