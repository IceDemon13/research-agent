using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public sealed class NpAreaDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("area_id")]
        public int? AreaId { get; set; }
    }
}