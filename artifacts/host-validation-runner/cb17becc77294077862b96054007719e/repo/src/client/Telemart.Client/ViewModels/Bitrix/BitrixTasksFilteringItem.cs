using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class BitrixTasksFilteringItem : FilteringItemBase
    {
        public BitrixTasksFilteringItem(string bitrixIds, IReadOnlyCollection<int> roles, IReadOnlyCollection<int> states)
        {
            BitrixIds = bitrixIds;
            Roles = roles;
            States = states;
        }

        [FilteringItemProperty("bitrix_ids")]
        public string BitrixIds { get; set; }

        [FilteringItemProperty("roles")]
        public IReadOnlyCollection<int> Roles { get; set; }

        [FilteringItemProperty("states")]
        public IReadOnlyCollection<int> States { get; set; }
    }
}
