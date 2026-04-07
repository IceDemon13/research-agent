using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class AbcClassPriceDto
    {
        [DataMember(Order = 1)]
        [JsonProperty("abc_id")]
        public int AbcId { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("abc_name")]
        public string AbcName { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }
    }
}