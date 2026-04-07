using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Notification
{
    public class NotificationsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("employee_id")]
        public int? EmployeeId { get; init; }

        [FilteringItemProperty("notification_type_id")]
        public int? NotificationTypeId { get; init; }

        [FilteringItemProperty("document_id")]
        public int? DocumentId { get; init; }

        [FilteringItemProperty("entity_id")]
        public int? EntityId { get; init; }

        [FilteringItemProperty("created_on_from")]
        public DateTime? CreatedOnFrom { get; init; }

        [FilteringItemProperty("created_on_to")]
        public DateTime? CreatedOnTo { get; init; }

        [FilteringItemProperty("target_ids")]
        public int[] TargetIds { get; init; }
    }
}