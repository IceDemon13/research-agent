using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SalesMapDto
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("carry_name")]
        public string CarryName { get; set; }

        [JsonProperty("orders_count")]
        public int OrdersCount { get; set; }
    }
}