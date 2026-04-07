using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public class UpAreaDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }
    }
}