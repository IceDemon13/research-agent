using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class QueryServiceCenter : QueryEntityRequestBase<ServiceCenterDto>
    {
        public QueryServiceCenter(int id)
            : base(ApiResources.ServiceCenters, id)
        {
        }
    }
}