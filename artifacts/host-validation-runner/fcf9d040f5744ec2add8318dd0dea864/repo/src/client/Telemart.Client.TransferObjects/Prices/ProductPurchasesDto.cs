using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class ProductPurchasesDto
    {
        [DataMember(Order = 1)]
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("avg_price_usd")]
        public double AvgPriceUsd { get; set; }
    }
}