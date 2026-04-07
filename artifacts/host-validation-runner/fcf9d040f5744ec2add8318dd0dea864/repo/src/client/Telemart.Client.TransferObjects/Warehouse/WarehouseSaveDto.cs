using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehouseSaveDto
    {
        [JsonProperty("active")]
        public int Active { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tags_header")]
        public string TagsHeader { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("max_package_weight")]
        public int MaxPackageWeight { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_assembly_id")]
        public int? EmployeeAssemblyId { get; set; }

        [JsonProperty("employee_additional_service_id")]
        public int EmployeeAdditionalServiceId { get; set; }

        [JsonProperty("buffer_warehouse_id")]
        public int? BufferWarehouseId { get; set; }

        [JsonProperty("assembly_warehouse_id")]
        public int? AssemblyWarehouseId { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("use_cells")]
        public bool UseCells { get; set; }

        [JsonProperty("auto_source")]
        public bool AutoSource { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("location_id")]
        public int? LocationId { get; set; }

        [JsonProperty("quota")]
        public TimeSpan? Quota { get; set; }

        [JsonProperty("performance_pattern_id")]
        public int? PerformancePatternId { get; set; }
    }
}