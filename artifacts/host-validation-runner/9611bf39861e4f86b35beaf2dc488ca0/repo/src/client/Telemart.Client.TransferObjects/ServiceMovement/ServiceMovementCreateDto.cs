using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceMovement
{
    public class ServiceMovementCreateDto
    {
        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; set; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; set; }

        [JsonProperty("date_receive")]
        public DateTime DateReceive { get; set; }

        [JsonProperty("service_request_ids")]
        public int[] ServiceRequestIds { get; set; }

        [JsonProperty("id_delivery_type")]
        public int? DeliveryTypeId { get; set; }

        [JsonProperty("id_carry")]
        public int? CarryId { get; set; }

        [JsonProperty("places")]
        public int? Places { get; set; }
    }
}