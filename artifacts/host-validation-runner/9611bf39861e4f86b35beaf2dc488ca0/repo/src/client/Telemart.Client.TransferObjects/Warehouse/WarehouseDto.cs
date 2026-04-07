using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.TransferObjects.Warehouse
{
    public class WarehouseDto : TrackableDtoBase<int>
    {
        [JsonProperty("active")]
        public int Active { get; init; }

        [JsonProperty("address")]
        public string Address { get; init; }

        [JsonProperty("address_ua")]
        public string AddressUa { get; init; }

        [JsonProperty("address_en")]
        public string AddressEn { get; init; }

        [JsonProperty("city_id")]
        public int CityId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("position")]
        public int Position { get; init; }

        [JsonProperty("tags_header")]
        public string TagsHeader { get; init; }

        [JsonProperty("house")]
        public string House { get; init; }

        [JsonProperty("np_street_ref")]
        public string NpStreetRef { get; init; }

        [JsonProperty("np_warehouse_ref")]
        public string NpWarehouseRef { get; init; }

        [JsonProperty("warehouse_performances")]
        public List<WarehousePerformanceDto> WarehousePerformances { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("max_package_weight")]
        public int MaxPackageWeight { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("employee_assembly_id")]
        public int? EmployeeAssemblyId { get; init; }

        [JsonProperty("location_id")]
        public int? LocationId { get; init; }

        [JsonProperty("employee_additional_service_id")]
        public int EmployeeAdditionalServiceId { get; init; }

        [JsonProperty("buffer_warehouse_id")]
        public int? BufferWarehouseId { get; init; }

        [JsonProperty("assembly_warehouse_id")]
        public int? AssemblyWarehouseId { get; init; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; init; }

        [JsonProperty("longitude")]
        public string Longitude { get; init; }

        [JsonProperty("latitude")]
        public string Latitude { get; init; }

        [JsonProperty("use_cells")]
        public bool UseCells { get; init; }

        [JsonProperty("info")]
        public string Info { get; init; }

        [JsonProperty("auto_source")]
        public bool AutoSource { get; init; }

        [JsonProperty("quota")]
        public TimeSpan? Quota { get; init; }

        [JsonProperty("performance_pattern_id")]
        public int? PerformancePatternId { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("treat_warnings_as_errors")]
        public bool TreatErrorsAsWarnings { get; init; }
    }
}