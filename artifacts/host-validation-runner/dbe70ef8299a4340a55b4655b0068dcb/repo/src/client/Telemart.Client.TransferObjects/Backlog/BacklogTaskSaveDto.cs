using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public class BacklogTaskSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("resolution_id")]
        public int? ResolutionId { get; set; }

        [JsonProperty("jira_id")]
        public string JiraId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }
    }
}