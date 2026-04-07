using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyService
{
    public class AssemblyServiceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("parent_assembly_service_id")]
        public int? ParentAssemblyServiceId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("subdivision_id")]
        public int? SubdivisionId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("order_state_id")]
        public int OrderStateId { get; set; }

        [JsonProperty("order_warehouse_id")]
        public int? OrderWarehouseId { get; set; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; set; }

        [JsonProperty("system_comment")]
        public string SystemComment { get; set; }

        [JsonProperty("decline_reason_id")]
        public int? DeclineReasonId { get; set; }

        [JsonProperty("order_delivery_time")]
        public DateTime? OrderDeliveryTime { get; set; }

        [JsonProperty("order_delivery_time_to")]
        public DateTime? OrderDeliveryTimeTo { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("start_test_on")]
        public DateTime? StartTestOn { get; set; }

        [JsonProperty("start_test_by")]
        public int? StartTestBy { get; set; }

        [JsonProperty("start_disassembly_on")]
        public DateTime? StartDisassemblyOn { get; set; }

        [JsonProperty("start_disassembly_by")]
        public int? StartDisassemblyBy { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("nomenclature_series")]
        public string NomenclatureSeries { get; set; }

        [JsonProperty("assembly_date")]
        public DateTime AssemblyDate { get; set; }

        [JsonProperty("arrived_on")]
        public DateTime? ArrivedOn { get; set; }

        [JsonProperty("started_on")]
        public DateTime? StartedOn { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("assembled_on")]
        public DateTime? AssembledOn { get; set; }

        [JsonProperty("assembled_by")]
        public int? AssembledBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("places")]
        public int? Places { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<AssemblyServiceProductDto> Products { get; set; }

        [JsonProperty("additional_services")]
        public List<AdditionalServiceProductDto> AdditionalServices { get; set; }
    }
}