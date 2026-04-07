using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCreateNomenclatureTaskDto
    {
        public TradeInCreateNomenclatureTaskDto(string taskComment)
        {
            TaskComment = taskComment;
        }

        [JsonProperty("task_comment")]
        public string TaskComment { get; }
    }
}