using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Common
{
    public class PromoFilteringItem : FilteringItemBase
    {
        public PromoFilteringItem(int subdivisionId, int[] productIds, bool active)
        {
            SubdivisionId = subdivisionId;
            ProductIds = productIds;
            Active = active;
        }

        [FilteringItemProperty("subdivision_id")]
        public int SubdivisionId { get; }

        [FilteringItemProperty("product_ids")]
        public int[] ProductIds { get; }

        [FilteringItemProperty("active")]
        public bool Active { get; }
    }
}
