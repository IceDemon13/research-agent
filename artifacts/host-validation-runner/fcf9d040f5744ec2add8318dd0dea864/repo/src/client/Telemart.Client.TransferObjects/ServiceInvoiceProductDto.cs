using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceInvoiceProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("service_repair_id")]
        public int ServiceRepairId { get; set; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }

        [JsonProperty("repair_invoice")]
        public string RepairInvoice { get; set; }

        [JsonProperty("product_name_ru")]
        public string ProductNameRu { get; set; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("defect")]
        public string Defect { get; set; }
    }
}