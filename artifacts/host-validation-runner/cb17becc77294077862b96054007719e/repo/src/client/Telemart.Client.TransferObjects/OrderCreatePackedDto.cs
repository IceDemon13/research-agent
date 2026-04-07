using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreatePackedDto : OrderCreateDto
    {
        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("location_id")]
        public int? LocationId { get; set; }

        [JsonProperty("serial_numbers")]
        public List<OrderCreatePackedSerialNumberDto> SerialNumbers { get; set; }
    }
}