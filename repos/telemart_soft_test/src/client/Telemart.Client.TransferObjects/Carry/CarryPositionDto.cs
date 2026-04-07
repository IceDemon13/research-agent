using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public class CarryPositionDto
    {
        public CarryPositionDto(int id, int position)
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