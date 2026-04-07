using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Notification;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class QueryNotifications : QueryEntitiesPagedRequestBase<NotificationDto>
    {
        public QueryNotifications(IFilteringItem filter)
            : base(filter, ApiResources.Notifications)
        {
        }
    }
}