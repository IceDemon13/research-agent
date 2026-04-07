using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("ids")]
        public string TaskNumbers { get; set; }

        [FilteringItemProperty("bitrix_ids")]
        public string BitrixIds { get; set; }

        [FilteringItemProperty("states")]
        public List<int> Statuses { get; set; }

        [FilteringItemProperty("author")]
        public List<int> AuthorIds { get; set; }

        [FilteringItemProperty("employee")]
        public List<int> EmployeeIds { get; set; }

        [FilteringItemProperty("category_ids")]
        public List<int> CategoryIds { get; set; }

        [FilteringItemProperty("quota_ids")]
        public List<int> QuotaIds { get; set; }
    }
}