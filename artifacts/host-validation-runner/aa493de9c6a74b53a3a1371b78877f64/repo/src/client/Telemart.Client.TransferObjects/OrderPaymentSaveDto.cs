using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderPaymentSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; set; }
    }
}