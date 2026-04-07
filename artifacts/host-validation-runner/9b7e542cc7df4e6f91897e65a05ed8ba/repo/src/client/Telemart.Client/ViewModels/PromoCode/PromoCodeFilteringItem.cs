using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("active")]
        public bool? Active { get; set; }

        [FilteringItemProperty("created_by_employee_ids")]
        public List<int> CreatedByEmployeeIds { get; set; }

        [FilteringItemProperty("type_ids")]
        public List<int> TypeIds { get; set; }

        [FilteringItemProperty("ids")]
        public List<int> Ids { get; set; }
    }
}