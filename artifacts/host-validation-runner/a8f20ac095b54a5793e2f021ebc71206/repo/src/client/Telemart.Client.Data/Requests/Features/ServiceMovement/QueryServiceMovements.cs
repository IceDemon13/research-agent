using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class QueryServiceMovements : QueryEntitiesPagedRequestBase<ServiceMovementSimpleDto>
    {
        public QueryServiceMovements(IFilteringItem filter)
            : base(filter, ApiResources.ServiceMovements)
        {
        }
    }
}
