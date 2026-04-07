using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestProductSnDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("sn")]
        public string SerialNumber { get; set; }

        [JsonProperty("defect")]
        public string Defect { get; set; }
    }
}