using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceMovement
{
    public class ServiceMovementProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }

        [JsonProperty("received")]
        public bool Received { get; set; }

        [JsonProperty("product_full_name")]
        public string ProductFullName { get; set; }

        [JsonProperty("product_full_name_ukr")]
        public string ProductFullNameUkr { get; set; }

        [JsonProperty("product_full_name_en")]
        public string ProductFullNameEn { get; set; }

        [JsonProperty("product_sn")]
        public string ProductSn { get; set; }
    }
}