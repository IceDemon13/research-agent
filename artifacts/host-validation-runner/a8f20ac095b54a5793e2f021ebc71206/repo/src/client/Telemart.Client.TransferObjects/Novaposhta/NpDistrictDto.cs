using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public sealed class NpDistrictDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("district_id")]
        public int? DistrictId { get; set; }
    }
}