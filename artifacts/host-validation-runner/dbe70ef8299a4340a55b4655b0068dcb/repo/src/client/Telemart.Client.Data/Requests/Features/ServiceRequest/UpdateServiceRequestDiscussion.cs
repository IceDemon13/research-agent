using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class UpdateServiceRequestDiscussion : UpdateEntityResultRequestBase<ServiceRequestDiscussionDto,
        UpdateServiceRequestDiscussion.ServiceRequestDiscussionUpdateDto>
    {
        public UpdateServiceRequestDiscussion(int serviceRequestId, int serviceRequestDiscussionId, string message)
            : base(
                new ServiceRequestDiscussionUpdateDto
                {
                    Id = serviceRequestDiscussionId,
                    Message = message
                },
                ApiResources.ServiceRequests,
                serviceRequestId,
                "discussions",
                serviceRequestDiscussionId)
        {
        }

        public class ServiceRequestDiscussionUpdateDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}