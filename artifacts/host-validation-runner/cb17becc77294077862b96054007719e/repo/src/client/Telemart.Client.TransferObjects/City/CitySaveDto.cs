using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.City
{
    public sealed class CitySaveDto
    {
        public CitySaveDto(
            int id,
            string name,
            string nameUkr,
            string nameEn,
            string npCityRef,
            string meCityRef,
            int? upCityId,
            int? uklonCityId,
            int position,
            int? areaId,
            int? districtId,
            IReadOnlyCollection<CityCarryDto> cityCarries)
        {
            Id = id;
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            NpCityRef = npCityRef;
            MeCityRef = meCityRef;
            Position = position;
            DistrictId = districtId;
            AreaId = areaId;
            CityCarries = cityCarries;
            UpCityId = upCityId;
            UklonCityId = uklonCityId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("np_city_ref")]
        public string NpCityRef { get; set; }

        [JsonProperty("me_city_ref")]
        public string MeCityRef { get; set; }

        [JsonProperty("up_city_id")]
        public int? UpCityId { get; set; }

        [JsonProperty("uklon_city_id")]
        public int? UklonCityId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("district_id")]
        public int? DistrictId { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }

        [JsonProperty("city_carries")]
        public IReadOnlyCollection<CityCarryDto> CityCarries { get; set; }
    }
}