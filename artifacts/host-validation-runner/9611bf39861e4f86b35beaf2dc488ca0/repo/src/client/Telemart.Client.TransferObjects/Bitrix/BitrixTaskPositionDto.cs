using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bitrix
{
    public class BitrixTaskPositionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}