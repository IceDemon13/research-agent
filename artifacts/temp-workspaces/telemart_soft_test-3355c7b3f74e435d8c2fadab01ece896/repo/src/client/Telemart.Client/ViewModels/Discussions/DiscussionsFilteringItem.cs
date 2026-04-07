using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Discussions
{
    public class DiscussionsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("ids")]
        public string Ids { get; init; }

        [FilteringItemProperty("state_ids")]
        public int[] StateIds { get; init; }

        [FilteringItemProperty("deadline_before")]
        public DateTime? DeadlineBefore { get; init; }

        [FilteringItemProperty("deadline_after")]
        public DateTime? DeadlineAfter { get; init; }

        [FilteringItemProperty("created_on_before")]
        public DateTime? CreatedOnBefore { get; init; }

        [FilteringItemProperty("created_on_after")]
        public DateTime? CreatedOnAfter { get; init; }

        [FilteringItemProperty("created_by")]
        public int? CreatedBy { get; init; }

        [FilteringItemProperty("executor_employee_id")]
        public int? ExecutorEmployeeId { get; init; }

        [FilteringItemProperty("co_executor_employee_id")]
        public int? CoExecutorEmployeeId { get; init; }

        [FilteringItemProperty("auditor_employee_id")]
        public int? AuditorEmployeeId { get; init; }

        [FilteringItemProperty("title")]
        public string Title { get; init; }

        [FilteringItemProperty("bitrix_id")]
        public string BitrixId { get; init; }

        [FilteringItemProperty("document_id")]
        public int? DocumentId { get; init; }

        [FilteringItemProperty("entity_id")]
        public int? EntityId { get; init; }
    }
}