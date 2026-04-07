using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public sealed class CarryProviderDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }
    }
}