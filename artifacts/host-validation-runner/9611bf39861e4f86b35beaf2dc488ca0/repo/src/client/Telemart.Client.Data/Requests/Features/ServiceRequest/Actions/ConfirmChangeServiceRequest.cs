using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class ConfirmChangeServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestConfirmReturnDto>
    {
        public ConfirmChangeServiceRequest(int serviceRequestId, decimal amount, int currencyId, string comment)
        : base(serviceRequestId, new ServiceRequestConfirmReturnDto(serviceRequestId, amount, currencyId, comment), ApiResources.ServiceRequests, "change")
        {
        }
    }
}