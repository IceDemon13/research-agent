using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AssemblyTest.Actions.ReorderAssemblyTests;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public sealed class ReorderAssemblyTests : CallEntityActionWithBodyRequestResultBase<object, ReorderTestsDto>
    {
        public ReorderAssemblyTests(int groupId, AssemblyTestPositionDto[] positions)
            : base(groupId, new ReorderTestsDto(groupId, positions), $"{ApiResources.AssemblyTests}/groups", "reorder_tests")
        {
        }

        public class ReorderTestsDto
        {
            public ReorderTestsDto(int groupId, AssemblyTestPositionDto[] positions)
            {
                Positions = positions;
                GroupId = groupId;
            }

            [JsonProperty("positions")]
            public AssemblyTestPositionDto[] Positions { get; set; }

            [JsonProperty("group_id")]
            public int GroupId { get; set; }
        }
    }
}