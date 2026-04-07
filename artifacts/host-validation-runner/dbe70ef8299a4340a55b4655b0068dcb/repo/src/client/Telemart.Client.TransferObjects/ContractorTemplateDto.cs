using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ContractorTemplateDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("client_id")]
        public int ClientId { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }
    }
}