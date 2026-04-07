using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
      public sealed class ConfirmTradeInServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ConfirmTradeInDto>
        {
            public ConfirmTradeInServiceRequest(int serviceRequestId, int bonusAmount)
            : base(serviceRequestId, new ConfirmTradeInDto(serviceRequestId, bonusAmount),  ApiResources.ServiceRequests, "tradein")
            {
            }
        }
}