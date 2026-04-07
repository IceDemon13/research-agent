using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeDocumentPlaceCreateDto
    {
        public MeDocumentPlaceCreateDto(decimal weight, decimal insurance)
        {
            Weight = weight;
            Insurance = insurance;
        }

        [JsonProperty("weight")]
        public decimal Weight { get; set; }

        [JsonProperty("insurance")]
        public decimal Insurance { get; set; }
    }
}