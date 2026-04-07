using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class MovementFastSendDto
    {
        public MovementFastSendDto(bool fastSale = false, bool system = false)
        {
            FastSale = fastSale;
            System = system;
        }

        [JsonProperty("fast_sale")]
        public bool FastSale { get; }

        [JsonProperty("system")]
        public bool System { get; }
    }
}