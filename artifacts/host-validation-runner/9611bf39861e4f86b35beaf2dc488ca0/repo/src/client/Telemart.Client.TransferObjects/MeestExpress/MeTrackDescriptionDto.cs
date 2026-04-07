using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeTrackDescriptionDto
    {
        [JsonProperty("descrRU")]
        public string DescriptionRu { get; set; }
    }
}