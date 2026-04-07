using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehouseCreateDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_assembly_id")]
        public int? EmployeeAssemblyId { get; set; }

        [JsonProperty("employee_additional_service_id")]
        public int EmployeeAdditionalServiceId { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }
}