using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public class ServiceRequestsGroupFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("finished")]
        public bool Finished { get; set; }
    }
}
