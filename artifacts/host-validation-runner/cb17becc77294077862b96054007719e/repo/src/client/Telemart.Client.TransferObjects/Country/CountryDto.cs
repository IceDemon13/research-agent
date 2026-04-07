using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Country
{
    public class CountryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }
    }
}