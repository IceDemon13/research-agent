using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class ConfirmServiceRepair : CallEntityActionRequestResultBase<ServiceRepairDto>
    {
        public ConfirmServiceRepair(int id)
            : base(id, ApiResources.ServiceRepairs, "confirm")
        {
        }
    }
}
