using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class TakeFromServiceCenter : CallEntityActionWithBodyRequestResultBase<ServiceRepairDto, ServiceRepairTakeFromServiceCenterDto>
    {
        public TakeFromServiceCenter(int serviceRepairId, ServiceRepairTakeFromServiceCenterDto dto)
            : base(serviceRepairId, dto, ApiResources.ServiceRepairs, "take")
        {
        }
    }
}