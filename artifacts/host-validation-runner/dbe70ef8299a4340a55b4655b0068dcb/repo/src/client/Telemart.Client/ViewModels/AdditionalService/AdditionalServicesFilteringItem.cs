using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class AdditionalServicesFilteringItem : FilteringItemBase
    {
        public AdditionalServicesFilteringItem(int[] appliedToProductIds = null, bool? active = null)
        {
            AppliedToProductIds = appliedToProductIds;
            Active = active;
        }

        [FilteringItemProperty("applied_to_product_ids")]
        public int[] AppliedToProductIds { get; set; }

        [FilteringItemProperty("active")]
        public bool? Active { get; set; }
    }
}
