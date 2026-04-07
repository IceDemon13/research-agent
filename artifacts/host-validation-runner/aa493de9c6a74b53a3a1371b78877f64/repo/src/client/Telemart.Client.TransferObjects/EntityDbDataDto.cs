using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class EntityDbDataDto
    {
        [JsonProperty("table_name")]
        public string TableName { get; init; }

        [JsonProperty("primary_key_name")]
        public string PrimaryKeyName { get; init; }
    }
}