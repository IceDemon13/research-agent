using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class ReorderAdditionalServiceGroups : CallEntityActionWithBodyRequestResultBase<object, ReorderAdditionalServiceGroups.ReorderAdditionalServiceGroupsDto>
    {
        public ReorderAdditionalServiceGroups(int groupId, int? parentGroupId, AdditionalServiceGroupPositionDto[] positions)
            : base(groupId, new ReorderAdditionalServiceGroupsDto(parentGroupId, positions), ApiResources.AdditionalServicesGroups, "reorder")
        {
        }

        public class ReorderAdditionalServiceGroupsDto
        {
            public ReorderAdditionalServiceGroupsDto(int? parentGroupId, AdditionalServiceGroupPositionDto[] positions)
            {
                Positions = positions;
                ParentGroupId = parentGroupId;
            }

            [JsonProperty("positions")]
            public AdditionalServiceGroupPositionDto[] Positions { get; set; }

            [JsonProperty("parent_group_id")]
            public int? ParentGroupId { get; set; }
        }
    }
}