using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class UnlockServiceRepair : UnlockRequestBase<ServiceRepairDto>
    {
        public UnlockServiceRepair(int id, bool force = false)
            : base(force, ApiResources.ServiceRepairs, id)
        {
        }
    }
}