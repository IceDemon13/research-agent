using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class ReconfirmServiceRepair : CallEntityActionRequestResultBase<ServiceRepairDto>
    {
        public ReconfirmServiceRepair(int id)
            : base(id, ApiResources.ServiceRepairs, "reconfirm")
        {
        }
    }
}
