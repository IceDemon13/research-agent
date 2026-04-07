using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NewPostWeekScheduleDto
    {
        [JsonProperty("monday")]
        public string Monday { get; set; }

        [JsonProperty("tuesday")]
        public string Tuesday { get; set; }

        [JsonProperty("wednesday")]
        public string Wednesday { get; set; }

        [JsonProperty("thursday")]
        public string Thursday { get; set; }

        [JsonProperty("friday")]
        public string Friday { get; set; }

        [JsonProperty("saturday")]
        public string Saturday { get; set; }

        [JsonProperty("sunday")]
        public string Sunday { get; set; }
    }
}