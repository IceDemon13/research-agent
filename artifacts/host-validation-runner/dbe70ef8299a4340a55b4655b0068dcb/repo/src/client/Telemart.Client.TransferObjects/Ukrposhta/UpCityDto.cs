using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpCityDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("up_district_id")]
        public int UpDistrictId { get; set; }

        [JsonProperty("up_area_id")]
        public int UpAreaId { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("district_name")]
        public string DistrictName { get; set; }

        [JsonProperty("district_name_ukr")]
        public string DistrictNameUkr { get; set; }

        [JsonProperty("area_name")]
        public string AreaName { get; set; }

        [JsonProperty("area_name_ukr")]
        public string AreaNameUkr { get; set; }
    }
}