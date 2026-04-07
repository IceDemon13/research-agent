using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CreateUklonOrderDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("uklon_address_id")]
        public string UklonAddressId { get; init; }

        [JsonProperty("uklon_address_name")]
        public string UklonAddressName { get; init; }
    }
}