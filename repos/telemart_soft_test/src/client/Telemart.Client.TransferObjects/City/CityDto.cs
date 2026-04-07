using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects.City
{
    public class CityDto : TrackableDtoBase<int>, IDictionaryItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("np_city_ref")]
        public string NpCityRef { get; init; }

        [JsonProperty("me_city_ref")]
        public string MeCityRef { get; init; }

        [JsonProperty("up_city_id")]
        public int? UpCityId { get; init; }

        [JsonProperty("uklon_city_id")]
        public int? UklonCityId { get; init; }

        [JsonProperty("district_id")]
        public int? DistrictId { get; init; }

        [JsonProperty("area_id")]
        public int? AreaId { get; init; }

        [JsonProperty("city_carries")]
        public IReadOnlyCollection<CityCarryDto> CityCarries { get; init; }
    }
}