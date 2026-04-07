using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public class CreateForeignCity : CreateEntityResultRequestBase<ForeignCityDto, CreateForeignCityDto>
    {
        public CreateForeignCity(string name, string nameUkr, string nameEn, int countryId)
            : base(new CreateForeignCityDto(name, nameUkr, nameEn, countryId), ApiResources.Cities, "foreign")
        {
        }
    }

    public class CreateForeignCityDto
    {
        public CreateForeignCityDto(string name, string nameUkr, string nameEn, int countryId)
        {
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            CountryId = countryId;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("country_id")]
        public int? CountryId { get; set; }
    }
}