using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class MoveAdditionalService : CallEntityActionWithBodyRequestResultBase<AdditionalServiceDto, MoveAdditionalService.MoveAdditionalServiceDto>
    {
        public MoveAdditionalService(int id, int? toGroupId)
            : base(id, new MoveAdditionalServiceDto(id, toGroupId), ApiResources.AdditionalServices, "move")
        {
        }

        public class MoveAdditionalServiceDto
        {
            public MoveAdditionalServiceDto(int id, int? toGroupId)
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