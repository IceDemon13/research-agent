using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceMovement
{
    public class ServiceMovementDto : ServiceMovementSimpleDto
    {
        [JsonProperty("products")]
        public ICollection<ServiceMovementProductDto> Products { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("id_delivery_type")]
        public int? DeliveryTypeId { get; set; }

        [JsonProperty("places")]
        public short? Places { get; set; }
    }
}