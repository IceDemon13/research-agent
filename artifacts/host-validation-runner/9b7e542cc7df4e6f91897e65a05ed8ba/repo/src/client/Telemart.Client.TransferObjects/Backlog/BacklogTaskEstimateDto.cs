using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public class BacklogTaskEstimateDto
    {
        public BacklogTaskEstimateDto(int id, int? estimate)
        {
            Id = id;
            Estimate = estimate;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("estimate")]
        public int? Estimate { get; set; }
    }
}