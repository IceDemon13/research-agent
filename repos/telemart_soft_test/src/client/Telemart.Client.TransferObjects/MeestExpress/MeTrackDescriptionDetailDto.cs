using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeTrackDescriptionDetailDto
    {
        [JsonProperty("descrRU")]
        public string DescriptionRu { get; set; }
    }
}