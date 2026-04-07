using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public sealed record ComplaintSourceDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}