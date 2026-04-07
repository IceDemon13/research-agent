using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ManufactorDto
    {
        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; init; }
    }
}