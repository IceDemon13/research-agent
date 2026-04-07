using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class ReorderAdditionalServices : CallEntityActionWithBodyRequestResultBase<object, ReorderAdditionalServices.ReorderAdditionalServicesDto>
    {
        public ReorderAdditionalServices(int groupId, AdditionalServicePositionDto[] positions)
            : base(groupId, new ReorderAdditionalServicesDto(groupId, positions), ApiResources.AdditionalServices, "reorder")
        {
        }

        public class ReorderAdditionalServicesDto
        {
            public ReorderAdditionalServicesDto(int groupId, AdditionalServicePositionDto[] positions)
            {
                Positions = positions;
                GroupId = groupId;
            }

            [JsonProperty("positions")]
            public AdditionalServicePositionDto[] Positions { get; set; }

            [JsonProperty("group_id")]
            public int GroupId { get; set; }
        }
    }
}