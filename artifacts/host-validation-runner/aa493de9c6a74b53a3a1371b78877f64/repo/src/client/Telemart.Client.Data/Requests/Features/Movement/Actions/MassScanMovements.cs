using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class MassScanMovements : CallActionWithBodyRequestResultBase<Result, MovementsMassScanDto>
    {
        public MassScanMovements(MovementsMassScanDto dto)
            : base(dto, ApiResources.Movements, "mass_scan")
        {
        }
    }
}