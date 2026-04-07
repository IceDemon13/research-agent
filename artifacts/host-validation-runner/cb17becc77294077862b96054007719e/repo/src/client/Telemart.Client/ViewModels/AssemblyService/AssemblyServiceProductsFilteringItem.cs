using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServiceProductsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("movement_ids")]
        public int[] MovementIds { get; set; }

        [FilteringItemProperty("assembly_service_state_ids")]
        public int[] AssemblyServiceStateIds { get; set; }
    }
}
