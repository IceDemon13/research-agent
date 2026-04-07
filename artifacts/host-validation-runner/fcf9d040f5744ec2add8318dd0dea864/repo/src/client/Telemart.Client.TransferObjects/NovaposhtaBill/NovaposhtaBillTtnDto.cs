using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaBill
{
    public class NovaposhtaBillTtnDto
    {
        [JsonProperty("ttn")]
        public string Ttn { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("weight_real")]
        public double WeightReal { get; set; }

        [JsonProperty("city_sender")]
        public string CitySender { get; set; }

        [JsonProperty("city_recipient")]
        public string CityRecipient { get; set; }
    }
}