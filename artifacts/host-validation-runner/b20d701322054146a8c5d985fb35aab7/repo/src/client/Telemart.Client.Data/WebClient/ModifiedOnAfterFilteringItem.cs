using System;

namespace Telemart.Client.Data.WebClient
{
    public class ModifiedOnAfterFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("modified_on_after")]
        public DateTime? ModifiedOnAfter { get; set; }
    }
}