using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderCancelDto
    {
        public OrderCancelDto(int id, int changeReasonId, string comment, int[] cellIds)
        {
            Id = id;
            CellIds = cellIds;
            OrderStateChangeReasonId = changeReasonId;
            Comment = comment;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("state_change_reason_id")]
        public int OrderStateChangeReasonId { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("cell_ids")]
        public int[] CellIds { get; init; }
    }
}