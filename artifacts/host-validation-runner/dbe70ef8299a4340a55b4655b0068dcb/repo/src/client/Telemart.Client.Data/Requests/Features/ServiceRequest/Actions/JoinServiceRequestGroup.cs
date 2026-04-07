using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class JoinServiceRequestGroup : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, JoinServiceRequestGroup.JoinServiceRequestGroupDto>
    {
        public JoinServiceRequestGroup(int fromId, int toId)
          : base(fromId, new JoinServiceRequestGroupDto(fromId, toId), ApiResources.ServiceRequests, "join")
        {
        }

        public class JoinServiceRequestGroupDto
        {
            public JoinServiceRequestGroupDto(int fromId, int toId)
            {
                this.FromId = fromId;
                this.ToId = toId;
            }

            [JsonProperty("from_id")]
            public int FromId { get; set; }

            [JsonProperty("to_id")]
            public int ToId { get; set; }
        }
    }
}
