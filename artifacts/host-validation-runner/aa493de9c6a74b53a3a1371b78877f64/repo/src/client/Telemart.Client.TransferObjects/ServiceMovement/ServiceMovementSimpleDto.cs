using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceMovement
{
    public class ServiceMovementSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; set; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("date_receive")]
        public DateTime DateReceive { get; set; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; set; }

        [JsonProperty("received_by")]
        public int? ReceivedBy { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("id_carry")]
        public int CarryId { get; set; }

        [JsonProperty("ttn")]
        public string TrackNumber { get; set; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}