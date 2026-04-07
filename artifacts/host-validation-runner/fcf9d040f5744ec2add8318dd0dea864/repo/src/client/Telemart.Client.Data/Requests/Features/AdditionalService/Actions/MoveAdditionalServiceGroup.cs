using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class MoveAdditionalServiceGroup : CallEntityActionWithBodyRequestResultBase<AdditionalServiceGroupDto, MoveAdditionalServiceGroup.MoveAdditionalServiceGroupDto>
    {
        public MoveAdditionalServiceGroup(int id, int? toGroupId)
            : base(id, new MoveAdditionalServiceGroupDto(id, toGroupId), ApiResources.AdditionalServicesGroups, "move")
        {
        }

        public class MoveAdditionalServiceGroupDto
        {
            public MoveAdditionalServiceGroupDto(int id, int? toGroupId)
            {
                Id = id;
                ToGroupId = toGroupId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("to_group_id")]
            public int? ToGroupId { get; set; }
        }
    }
}