using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateMovementLogisticsDto
    {
        public UpdateMovementLogisticsDto(int id, int carryId, string ttn)
        {
            Id = id;
            CarryId = carryId;
            Ttn = ttn;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("ttn")]
        public string Ttn { get; set; }
    }
}