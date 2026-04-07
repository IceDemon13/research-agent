using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductChangeStateDto
    {
        public OrderProductChangeStateDto(int id, int stateId)
        {
            Id = id;
            StateId = stateId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }
    }
}