using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class ReceiveNotification : CallEntityActionRequestResultBase<object>
    {
        public ReceiveNotification(int notificationId)
            : base(notificationId, ApiResources.Notifications, "receive")
        {
        }
    }
}