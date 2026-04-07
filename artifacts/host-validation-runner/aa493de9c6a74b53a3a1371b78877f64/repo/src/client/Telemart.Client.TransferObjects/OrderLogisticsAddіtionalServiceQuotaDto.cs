using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderLogisticsAddіtionalServiceQuotaDto
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("total")]
        public TimeSpan? Total { get; set; }

        [JsonProperty("used")]
        public TimeSpan? Used { get; set; }
    }
}