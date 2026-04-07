using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Bitrix;

namespace Telemart.Client.Data.Requests.Features.Bitrix.Actions
{
    public class MoveBitrixTask : CallEntityActionWithBodyRequestResultBase<List<BitrixTaskPositionDto>, MoveBitrixTask.BitrixTaskMoveDto>
    {
        public MoveBitrixTask(int taskId, int position)
            : base(taskId, new BitrixTaskMoveDto(taskId, position), ApiResources.BitrixTasks, "move")
        {
        }

        public class BitrixTaskMoveDto
        {
            public BitrixTaskMoveDto(int id, int position)
            {
                Id = id;
                Position = position;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("position")]
            public int Position { get; set; }
        }
    }
}