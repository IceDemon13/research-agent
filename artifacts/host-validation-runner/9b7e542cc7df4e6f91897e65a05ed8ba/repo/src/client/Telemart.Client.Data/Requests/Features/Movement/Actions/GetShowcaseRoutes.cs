using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public class GetShowcaseRoutes : CallActionRequestBase<IReadOnlyCollection<MovementShowcaseDataDto>>
    {
        public GetShowcaseRoutes()
            : base(ApiResources.Movements, "showcase_routes")
        {
        }
    }
}