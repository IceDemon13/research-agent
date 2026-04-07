using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeCityDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("area_ref")]
        public string AreaId { get; set; }

        [JsonProperty("district_ref")]
        public string DistrictId { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("district_name")]
        public string DistrictName { get; set; }

        [JsonProperty("district_name_ua")]
        public string DistrictNameUa { get; set; }

        [JsonProperty("area_name")]
        public string AreaName { get; set; }

        [JsonProperty("area_name_ua")]
        public string AreaNameUa { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}