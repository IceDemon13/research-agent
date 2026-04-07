using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssembledComputersFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("order_id")]
        public int? OrderId { get; init; }

        [FilteringItemProperty("nomenclature_series")]
        public string NomenclatureSeries { get; init; }
    }
}