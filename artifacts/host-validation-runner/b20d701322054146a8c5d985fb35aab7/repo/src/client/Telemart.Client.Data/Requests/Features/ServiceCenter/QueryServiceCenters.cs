using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class QueryServiceCenters : QueryEntitiesPagedRequestBase<ServiceCenterDto>
    {
        public QueryServiceCenters()
            : base(null, ApiResources.ServiceCenters)
        {
        }
    }
}
