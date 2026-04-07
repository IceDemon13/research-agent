using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public sealed class MeDistrictDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("district_id")]
        public int? DistrictId { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }
    }
}