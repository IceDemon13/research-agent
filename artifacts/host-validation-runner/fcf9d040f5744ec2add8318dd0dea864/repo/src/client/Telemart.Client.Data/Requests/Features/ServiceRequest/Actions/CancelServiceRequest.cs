using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CancelServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, CancelServiceRequest.ServiceRequestCancelDto>
    {
        public CancelServiceRequest(int serviceRequestId, string reason)
            : base(serviceRequestId, new ServiceRequestCancelDto { Id = serviceRequestId, Reason = reason }, ApiResources.ServiceRequests, "cancel")
        {
        }

        public class ServiceRequestCancelDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("reason")]
            public string Reason { get; set; }
        }
    }
}