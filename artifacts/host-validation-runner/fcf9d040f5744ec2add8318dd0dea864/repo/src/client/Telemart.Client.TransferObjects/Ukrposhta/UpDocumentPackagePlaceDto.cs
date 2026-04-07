using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpDocumentPackagePlaceDto
    {
        public UpDocumentPackagePlaceDto(decimal weight, decimal lenght, decimal width, decimal height)
        {
            Weight = weight;
            Lenght = lenght;
            Width = width;
            Height = height;
        }

        [JsonProperty("weight")]
        public decimal Weight { get; set; }

        [JsonProperty("lenght")]
        public decimal Lenght { get; set; }

        [JsonProperty("width")]
        public decimal Width { get; set; }

        [JsonProperty("height")]
        public decimal Height { get; set; }
    }
}