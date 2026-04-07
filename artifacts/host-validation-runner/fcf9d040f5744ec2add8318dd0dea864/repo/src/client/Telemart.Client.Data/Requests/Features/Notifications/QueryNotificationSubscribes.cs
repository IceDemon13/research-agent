using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Notification;

namespace Telemart.Client.Data.Requests.Features.Notifications
{
    public sealed class QueryNotificationSubscribes : QueryEntitiesRequestBase<NotificationSubscribeDto>
    {
        public QueryNotificationSubscribes(int employeeId)
        : base(new NotificationSubscribesFilteringItem(employeeId), $"{ApiResources.Notifications}/subscribes")
        {
        }

        private class NotificationSubscribesFilteringItem : FilteringItemBase
        {
            public NotificationSubscribesFilteringItem(int? employeeId)
            {
                EmployeeId = employeeId;
            }

            [FilteringItemProperty("employee_id")]
            public int? EmployeeId { get; }
        }
    }
}