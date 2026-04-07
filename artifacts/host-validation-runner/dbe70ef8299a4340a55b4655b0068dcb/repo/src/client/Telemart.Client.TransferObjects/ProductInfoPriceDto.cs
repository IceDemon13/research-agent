using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoPriceDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uah")]
        public decimal Uah { get; set; }

        [JsonProperty("usd")]
        public decimal Usd { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }
    }
}