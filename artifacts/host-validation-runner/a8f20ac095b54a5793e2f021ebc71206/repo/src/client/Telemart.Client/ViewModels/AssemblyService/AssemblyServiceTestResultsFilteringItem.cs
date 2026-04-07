using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServiceTestResultsFilteringItem : FilteringItemBase
    {
        public AssemblyServiceTestResultsFilteringItem(int assemblyServiceId)
        {
            AssemblyServiceId = assemblyServiceId;
        }

        [FilteringItemProperty("assembly_service_id")]
        public int AssemblyServiceId { get; set; }
    }
}
