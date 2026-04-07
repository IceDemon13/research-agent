using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderSaveDto
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("products")]
        public List<OrderProductSaveDto> OrderProducts { get; set; }

        [JsonProperty("folders")]
        public List<OrderFolderSaveDto> Folders { get; set; }

        [JsonProperty("promo_codes")]
        public List<OrderProductPromoCodeSaveDto> PromoCodes { get; set; }

        [JsonProperty("options")]
        public OrderOptionsDto Options { get; set; }

        [JsonProperty("buffer_warehouse_id")]
        public int? BufferWarehouseId { get; set; }

        [JsonProperty("assembly_warehouse_id")]
        public int? AssemblyWarehouseId { get; set; }

        [JsonProperty("additional_service_warehouse_id")]
        public int? AdditionalServiceWarehouseId { get; set; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; set; }

        [JsonProperty("order_source_id")]
        public int? OrderSourceId { get; set; }
    }
}