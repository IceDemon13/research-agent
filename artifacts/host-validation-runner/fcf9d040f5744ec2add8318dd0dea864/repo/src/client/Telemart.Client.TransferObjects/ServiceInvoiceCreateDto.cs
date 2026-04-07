using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceInvoiceCreateDto
    {
        [JsonProperty("service_center_id")]
        public int ServiceCenterId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("send_date")]
        public DateTime SendDate { get; set; }
    }
}