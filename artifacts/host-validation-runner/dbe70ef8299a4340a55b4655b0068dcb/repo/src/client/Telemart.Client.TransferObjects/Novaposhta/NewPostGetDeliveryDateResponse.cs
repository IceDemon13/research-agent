using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NewPostGetDeliveryDateResponse
    {
        [JsonProperty("delivery_date")]
        public DateTime DeliveryDate { get; set; }
    }
}