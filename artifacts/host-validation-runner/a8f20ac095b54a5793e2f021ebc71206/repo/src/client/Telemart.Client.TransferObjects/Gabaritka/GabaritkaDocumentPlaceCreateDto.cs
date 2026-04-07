using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Gabaritka
{
    public sealed class GabaritkaDocumentPlaceCreateDto
    {
        public GabaritkaDocumentPlaceCreateDto(decimal weight, decimal insurance)
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