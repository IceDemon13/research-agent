using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCancelInfoDto
    {
        [JsonProperty("order")]
        public OrderDto Order { get; init; }

        [JsonProperty("new_state_id")]
        public int NewStateId { get; init; }

        [JsonProperty("reasons")]
        public OrderStateChangeReasonDto[] Reasons { get; init; }

        [JsonIgnore]
        public int[] CellIds { get; set; }
    }
}