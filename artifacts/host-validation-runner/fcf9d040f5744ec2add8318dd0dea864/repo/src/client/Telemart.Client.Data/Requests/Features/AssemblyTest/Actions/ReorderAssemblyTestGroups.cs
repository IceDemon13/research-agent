using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AssemblyTest.Actions.ReorderAssemblyTestGroups;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public sealed class ReorderAssemblyTestGroups : CallEntityActionWithBodyRequestResultBase<object, ReorderTestGroupsDto>
    {
        public ReorderAssemblyTestGroups(int groupId, int? parentGroupId, AssemblyTestGroupPositionDto[] positions)
            : base(groupId, new ReorderTestGroupsDto(parentGroupId, positions), $"{ApiResources.AssemblyTests}/groups", "reorder")
        {
        }

        public class ReorderTestGroupsDto
        {
            public ReorderTestGroupsDto(int? parentGroupId, AssemblyTestGroupPositionDto[] positions)
            {
                Positions = positions;
                ParentGroupId = parentGroupId;
            }

            [JsonProperty("positions")]
            public AssemblyTestGroupPositionDto[] Positions { get; set; }

            [JsonProperty("parent_group_id")]
            public int? ParentGroupId { get; set; }
        }
    }
}