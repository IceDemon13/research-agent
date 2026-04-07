using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderConfigurationDto
    {
        [JsonProperty("dont_call_me_amount")]
        public int DontCallMeAmount { get; set; }

        [JsonProperty("dont_call_me_hashtag_ids")]
        public List<int> DontCallMeHashtagIds { get; set; }

        [JsonProperty("dont_call_me_payment_ids")]
        public List<int> DontCallMePaymentIds { get; set; }

        [JsonProperty("dont_call_me_carry_ids")]
        public List<int> DontCallMeCarryIds { get; set; }
    }
}