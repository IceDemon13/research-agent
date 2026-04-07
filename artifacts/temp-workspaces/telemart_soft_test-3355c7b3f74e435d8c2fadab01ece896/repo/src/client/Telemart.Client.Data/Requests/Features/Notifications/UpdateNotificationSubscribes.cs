using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class UpdateNotificationSubscribes : CallActionWithBodyRequestBase<Result, UpdateNotificationSubscribesDto>
    {
        public UpdateNotificationSubscribes(UpdateNotificationSubscribesDto dto)
            : base(dto, ApiResources.Notifications, "subscribes")
        {
        }
    }
}