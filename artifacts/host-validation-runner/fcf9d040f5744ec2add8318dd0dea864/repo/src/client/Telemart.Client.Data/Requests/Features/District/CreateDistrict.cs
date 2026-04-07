using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.District
{
    public sealed class CreateDistrict : CreateEntityResultRequestBase<DistrictDto, CreateDistrict.DistrictCreateDto>
    {
        public CreateDistrict(string name, string nameUkr, string nameEn, bool isActive, int areaId, string meDistrictRef, int? upDistrictId)
        : base(new DistrictCreateDto(name, nameUkr, nameEn, isActive, areaId, meDistrictRef, upDistrictId), ApiResources.Districts)
        {
        }

        public sealed class DistrictCreateDto
        {
            public DistrictCreateDto(string name, string nameUkr, string nameEn, bool isActive, int areaId, string meDistrictRef, int? upDistrictId)
            {
                Name = name;
                NameUkr = nameUkr;
                NameEn = nameEn;
                Active = isActive;
                AreaId = areaId;
                MeDistrictRef = meDistrictRef;
                UpDistrictId = upDistrictId;
            }

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
}