using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class AcceptConfirmServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public AcceptConfirmServiceRequest(int id)
            : base(id, ApiResources.ServiceRequests, "accept_confirm")
        {
        }
    }
}
