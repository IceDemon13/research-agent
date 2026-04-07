using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeGetDeliveryDateResponse
    {
        [JsonProperty("delivery_date")]
        public DateTime DeliveryDate { get; set; }
    }
}