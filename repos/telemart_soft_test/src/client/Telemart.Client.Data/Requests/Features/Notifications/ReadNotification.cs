using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class ReadNotification : CallEntityActionRequestResultBase<object>
    {
        public ReadNotification(int notificationId)
            : base(notificationId, ApiResources.Notifications, "read")
        {
        }
    }
}