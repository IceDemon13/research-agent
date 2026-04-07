using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpDistrictDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("up_area_id")]
        public int UpAreaId { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("district_id")]
        public int? DistrictId { get; set; }
    }
}