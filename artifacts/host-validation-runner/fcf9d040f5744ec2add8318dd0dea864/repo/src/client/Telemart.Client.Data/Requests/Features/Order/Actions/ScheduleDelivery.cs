using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class ScheduleDelivery : CallActionWithBodyRequestResultBase<object, ScheduleDeliveryDto>
    {
        public ScheduleDelivery(ScheduleDeliveryDto dto)
            : base(dto, ApiResources.Orders, "schedule_delivery")
        {
        }
    }
}