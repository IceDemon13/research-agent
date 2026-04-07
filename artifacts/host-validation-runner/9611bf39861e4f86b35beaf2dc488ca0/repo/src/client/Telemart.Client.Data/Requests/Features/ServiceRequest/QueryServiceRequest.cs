using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequest : QueryEntityRequestBase<ServiceRequestDto>
    {
        public QueryServiceRequest(int id)
            : base(ApiResources.ServiceRequests, id)
        {
        }
    }
}