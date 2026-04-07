using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public sealed class MeAreaDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }

        [JsonProperty("description_ru")]
        public string DescriptionRu { get; set; }

        [JsonProperty("description_ua")]
        public string DescriptionUa { get; set; }
    }
}