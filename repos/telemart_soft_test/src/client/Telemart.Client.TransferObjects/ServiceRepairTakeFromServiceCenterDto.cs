using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRepairTakeFromServiceCenterDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_location_id")]
        public int WarehouseLocationId { get; set; }

        [JsonProperty("state")]
        public int StateId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("alternative")]
        public string Alternative { get; set; }

        [JsonProperty("act")]
        public string Act { get; set; }

        [JsonProperty("warranty_remove")]
        public bool WarrantyRemove { get; set; }

        [JsonProperty("service_center_conclusion")]
        public string ServiceCenterConclusion { get; set; }
    }
}