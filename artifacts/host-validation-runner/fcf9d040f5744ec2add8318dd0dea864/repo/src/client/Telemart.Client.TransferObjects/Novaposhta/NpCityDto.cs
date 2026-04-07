using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NpCityDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("area_ref")]
        public string AreaRef { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}