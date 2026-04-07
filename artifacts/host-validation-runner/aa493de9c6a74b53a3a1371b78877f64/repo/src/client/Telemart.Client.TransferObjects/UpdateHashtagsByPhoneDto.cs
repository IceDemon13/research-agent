using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UpdateHashtagsByPhoneDto
    {
        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("hashtag_ids")]
        public IReadOnlyCollection<int> HashtagIds { get; set; }
    }
}