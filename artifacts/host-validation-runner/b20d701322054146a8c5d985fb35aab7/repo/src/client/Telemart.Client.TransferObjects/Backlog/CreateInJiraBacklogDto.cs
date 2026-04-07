using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public sealed record CreateInJiraBacklogDto
    {
        [JsonProperty("jira_id")]
        public string JiraId { get; init; }

        [JsonProperty("estimate")]
        public int? Estimate { get; init; }
    }
}