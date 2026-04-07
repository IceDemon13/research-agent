using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestsGroup : QueryEntitiesRequestBase<ServiceRequestGroupDto>
    {
        public QueryServiceRequestsGroup(IFilteringItem filter)
            : base(filter, ApiResources.ServiceRequestsGroups)
        {
        }
    }
}
