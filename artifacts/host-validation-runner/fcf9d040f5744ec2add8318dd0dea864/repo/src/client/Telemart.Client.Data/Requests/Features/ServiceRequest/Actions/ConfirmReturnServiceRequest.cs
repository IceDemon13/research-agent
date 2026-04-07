using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class ConfirmReturnServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestConfirmReturnDto>
    {
        public ConfirmReturnServiceRequest(int serviceRequestId, decimal amount, int currencyId, string comment, RefundRequisitesDto refundRequisitesDto)
        : base(serviceRequestId, new ServiceRequestConfirmReturnDto(serviceRequestId, amount, currencyId, comment, refundRequisitesDto), ApiResources.ServiceRequests, "return")
        {
        }
    }
}