using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public sealed class CreateCity : CreateEntityResultRequestBase<CityDto, CreateCity.CityCreateDto>
    {
        public CreateCity(string name, string nameUkr, string nameEn)
            : base(new CityCreateDto(name, nameUkr, nameEn), ApiResources.Cities)
        {
        }

        public class CityCreateDto
        {
            public CityCreateDto(string name, string nameUkr, string nameEn)
            {
                Name = name;
                NameUkr = nameUkr;
                NameEn = nameEn;
            }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("name_ukr")]
            public string NameUkr { get; set; }

            [JsonProperty("name_en")]
            public string NameEn { get; set; }
        }
    }
}