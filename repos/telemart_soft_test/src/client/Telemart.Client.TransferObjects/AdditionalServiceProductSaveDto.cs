using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProductSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("scanned")]
        public bool Scanned { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("guest_product")]
        public GuestProductDto GuestProduct { get; set; }

        [JsonProperty("consumable_products")]
        public IReadOnlyCollection<AdditionalServiceProductConsumableSaveDto> ConsumableProducts { get; init; }
    }
}