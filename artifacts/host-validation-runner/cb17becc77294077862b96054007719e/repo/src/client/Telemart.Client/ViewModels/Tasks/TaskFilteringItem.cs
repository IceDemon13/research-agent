using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Tasks
{
    public class TaskFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("ids")]
        public string Numbers { get; set; }

        [FilteringItemProperty("states")]
        public IReadOnlyCollection<int> States { get; set; }

        [FilteringItemProperty("types")]
        public IReadOnlyCollection<int> Types { get; set; }

        [FilteringItemProperty("all_tasks")]
        public bool AllTasks { get; set; }
    }
}