using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public sealed class ReportDatabaseDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}