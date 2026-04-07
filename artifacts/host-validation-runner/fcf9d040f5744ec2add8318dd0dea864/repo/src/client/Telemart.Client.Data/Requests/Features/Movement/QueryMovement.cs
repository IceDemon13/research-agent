using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class QueryMovement : QueryEntityRequestBase<MovementDto>
    {
        public QueryMovement(int id)
            : base(ApiResources.Movements, id)
        {
        }
    }
}