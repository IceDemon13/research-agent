using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureGroupPositionDto
    {
        public FeatureGroupPositionDto(int groupId, int position)
        {
            GroupId = groupId;
            Position = position;
        }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}