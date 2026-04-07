using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class CancelServiceRepair : CallEntityActionRequestResultBase<ServiceRepairDto>
    {
        public CancelServiceRepair(int serviceRepairId)
            : base(serviceRepairId, ApiResources.ServiceRepairs, "cancel")
        {
        }
    }
}