using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.Movement.Actions.SendMovement;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class SendMovement : CallEntityActionWithBodyRequestResultBase<MovementDto, MovementSendDto>
    {
        public SendMovement(int id, int places)
            : base(id, new MovementSendDto(id, places), ApiResources.Movements, "send")
        {
        }

        public class MovementSendDto
        {
            public MovementSendDto(int id, int places)
            {
                Id = id;
                Places = places;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("places")]
            public int Places { get; set; }
        }
    }
}