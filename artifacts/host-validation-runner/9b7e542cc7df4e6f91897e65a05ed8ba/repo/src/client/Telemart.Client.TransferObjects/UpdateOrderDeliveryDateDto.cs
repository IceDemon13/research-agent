using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UpdateOrderDeliveryDateDto
    {
        [JsonProperty("delivery_from")]
        public DateTime? DeliveryDateFrom { get; set; }

        [JsonProperty("delivery_to")]
        public DateTime? DeliveryDateTo { get; set; }

        [JsonProperty("order_state_change_reason_id")]
        public int? OrderStateChangeReasonId { get; set; }
    }
}