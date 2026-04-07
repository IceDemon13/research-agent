using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PriceDto
    {
        [JsonProperty("uah")]
        public decimal Uah { get; set; }

        [JsonProperty("usd")]
        public decimal Usd { get; set; }
    }
}