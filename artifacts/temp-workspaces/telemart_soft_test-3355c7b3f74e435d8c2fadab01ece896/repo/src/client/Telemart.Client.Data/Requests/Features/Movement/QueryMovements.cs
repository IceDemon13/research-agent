using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class QueryMovements : QueryEntitiesPagedRequestBase<MovementDto>
    {
        public QueryMovements(IFilteringItem filter)
            : base(filter, ApiResources.Movements)
        {
        }
    }
}
