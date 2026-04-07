using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class QueryContractorContactPositions : QueryEntitiesRequestBase<ContractorContactPositionDto>
    {
        public QueryContractorContactPositions()
            : base($"{ApiResources.Contractors}/contacts/positions")
        {
        }
    }
}