using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PhoneHistoryResultDto
    {
        [JsonProperty("phones")]
        public string[] Phones { get; set; }

        [JsonProperty("plus_hashtags")]
        public List<string> PlusHashtagNames { get; set; }

        [JsonProperty("minus_hashtags")]
        public List<string> MinusHashtagNames { get; set; }

        [JsonProperty("history")]
        public List<PhoneHistoryDto> History { get; set; }

        [JsonProperty("customers")]
        public PhoneHistoryCustomerDto[] Customers { get; set; }
    }
}