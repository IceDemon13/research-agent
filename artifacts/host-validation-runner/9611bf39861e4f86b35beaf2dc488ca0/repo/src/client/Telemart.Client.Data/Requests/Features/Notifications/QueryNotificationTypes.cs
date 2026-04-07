using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Notification;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class QueryNotificationTypes : QueryEntitiesRequestBase<NotificationTypeDto>
    {
        public QueryNotificationTypes()
            : base($"{ApiResources.Notifications}/types")
        {
        }
    }
}