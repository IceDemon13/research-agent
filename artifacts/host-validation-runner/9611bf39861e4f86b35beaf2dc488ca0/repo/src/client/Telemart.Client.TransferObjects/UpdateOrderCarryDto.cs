using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateOrderCarryDto
    {
        public UpdateOrderCarryDto(int carryId)
        {
            CarryId = carryId;
        }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }
    }
}