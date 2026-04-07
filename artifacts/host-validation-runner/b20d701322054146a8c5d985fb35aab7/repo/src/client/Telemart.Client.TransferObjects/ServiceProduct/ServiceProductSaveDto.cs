using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceProduct
{
    public class ServiceProductSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("service_act_number")]
        public string ServiceActNumber { get; set; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}