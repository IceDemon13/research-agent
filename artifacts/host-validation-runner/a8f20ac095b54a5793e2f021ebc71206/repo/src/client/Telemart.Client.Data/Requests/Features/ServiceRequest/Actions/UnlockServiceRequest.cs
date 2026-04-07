using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class UnlockServiceRequest : UnlockRequestBase<ServiceRequestDto>
    {
        public UnlockServiceRequest(int id, bool force = false)
            : base(force, ApiResources.ServiceRequests, id)
        {
        }
    }
}
