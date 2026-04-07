using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.SalesMap
{
    public class SalesMapFilteringItem : FilteringItemBase
    {
        public SalesMapFilteringItem(DateTime? deliveryTimeAfter, DateTime? deliveryTimeBefore, int[] carryIds)
        {
            DeliveryTimeAfter = deliveryTimeAfter;
            DeliveryTimeBefore = deliveryTimeBefore;
            CarryIds = carryIds;
        }

        [FilteringItemProperty("delivery_time_after")]
        public DateTime? DeliveryTimeAfter { get; }

        [FilteringItemProperty("delivery_time_before")]
        public DateTime? DeliveryTimeBefore { get; }

        [FilteringItemProperty("carry_ids")]
        public int[] CarryIds { get; }
    }
}
