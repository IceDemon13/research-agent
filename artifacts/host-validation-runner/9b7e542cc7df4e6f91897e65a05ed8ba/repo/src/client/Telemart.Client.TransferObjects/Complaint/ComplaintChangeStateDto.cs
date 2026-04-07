using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public class ComplaintChangeStateDto
    {
        public ComplaintChangeStateDto(int id, int stateId, string resolution)
        {
            Id = id;
            StateId = stateId;
            Resolution = resolution;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }
    }
}