using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair
{
    public sealed class QueryServiceRepairs : QueryEntitiesPagedRequestBase<ServiceRepairDto>
    {
        public QueryServiceRepairs(IFilteringItem filter)
            : base(filter, ApiResources.ServiceRepairs)
        {
        }
    }
}