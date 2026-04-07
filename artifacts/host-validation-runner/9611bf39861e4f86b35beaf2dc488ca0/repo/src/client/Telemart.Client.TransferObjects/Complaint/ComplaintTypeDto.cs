using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public class ComplaintTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}