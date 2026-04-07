using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class QueryServiceMovement : QueryEntityRequestBase<ServiceMovementDto>
    {
        public QueryServiceMovement(int id)
            : base(ApiResources.ServiceMovements, id)
        {
        }
    }
}