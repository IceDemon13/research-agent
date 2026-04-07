using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class DenyConfirmServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public DenyConfirmServiceRequest(int id)
            : base(id, ApiResources.ServiceRequests, "deny_confirm")
        {
        }
    }
}
