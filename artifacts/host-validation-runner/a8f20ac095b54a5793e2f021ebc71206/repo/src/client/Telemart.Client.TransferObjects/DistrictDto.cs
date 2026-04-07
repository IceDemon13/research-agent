using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class DistrictDto
    {
        public DistrictDto(
            int id,
            string name,
            string nameUkr,
            string nameEn,
            bool active,
            int areaId,
            string meDistrictRef,
            int? upDistrictId)
        {
            Id = id;
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            Active = active;
            AreaId = areaId;
            MeDistrictRef = meDistrictRef;
            UpDistrictId = upDistrictId;
        }

        public DistrictDto()
        {
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("area_id")]
        public int AreaId { get; set; }

        [JsonProperty("me_district_ref")]
        public string MeDistrictRef { get; set; }

        [JsonProperty("up_district_id")]
        public int? UpDistrictId { get; set; }
    }
}