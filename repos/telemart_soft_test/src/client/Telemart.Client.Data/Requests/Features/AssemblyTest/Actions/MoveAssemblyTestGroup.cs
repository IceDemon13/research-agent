using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public class MoveAssemblyTestGroup : CallEntityActionWithBodyRequestResultBase<AssemblyTestGroupDto, MoveAssemblyTestGroup.MoveAssemblyTestGroupDto>
    {
        public MoveAssemblyTestGroup(int id, int? toGroupId)
            : base(id, new MoveAssemblyTestGroupDto(id, toGroupId), ApiResources.AssemblyTestGroups, "move")
        {
        }

        public class MoveAssemblyTestGroupDto
        {
            public MoveAssemblyTestGroupDto(int id, int? toGroupId)
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