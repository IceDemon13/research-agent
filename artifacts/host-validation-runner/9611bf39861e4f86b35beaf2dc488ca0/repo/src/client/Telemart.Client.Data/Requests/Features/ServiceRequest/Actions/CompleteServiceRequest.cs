using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CompleteServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, CompleteServiceRequest.ServiceRequestCompleteDto>
    {
        public CompleteServiceRequest(int serviceRequestId, string trackNumber)
            : base(serviceRequestId, new ServiceRequestCompleteDto { Id = serviceRequestId, TrackNumber = trackNumber }, ApiResources.ServiceRequests, "complete")
        {
        }

        public sealed class ServiceRequestCompleteDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("track_number")]
            public string TrackNumber { get; set; }
        }
    }
}