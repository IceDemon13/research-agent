using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProductDto : ILocalіzableEntity
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; set; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; set; }

        [JsonProperty("parent_order_product_id")]
        public int? ParentOrderProductId { get; set; }

        [JsonProperty("parent_order_product_product_id")]
        public int? ParentOrderProductProductId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("order_warehouse_id")]
        public int OrderWarehouseId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("modified_by")]
        public int? ModifiedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("order_delivery_time_to")]
        public DateTime? OrderDeliveryTimeTo { get; set; }

        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; set; }

        [JsonProperty("system_comment")]
        public string SystemComment { get; set; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("order_state_id")]
        public int OrderStateId { get; set; }

        [JsonProperty("additional_service_id")]
        public int AdditionalServiceId { get; set; }

        [JsonProperty("additional_service_name")]
        public string AdditionalServiceNameRu { get; set; }

        [JsonProperty("additional_service_name_ua")]
        public string AdditionalServiceNameUa { get; set; }

        [JsonProperty("additional_service_name_en")]
        public string AdditionalServiceNameEn { get; set; }

        [JsonProperty("control_in_movements")]
        public bool ControlInMovements { get; set; }

        [JsonProperty("scanned")]
        public bool Scanned { get; set; }

        [JsonProperty("additional_service_product_id")]
        public int AdditionalServiceProductId { get; set; }

        [JsonProperty("require_products_to_provide")]
        public bool RequireProductsToProvide { get; set; }

        [JsonProperty("priority_type_id")]
        public int PriorityTypeId { get; set; }

        [JsonProperty("primary_additional_service_product_id")]
        public int? PrimaryAdditionalServiceProductId { get; set; }

        [JsonProperty("disassembly")]
        public bool Disassembly { get; set; }

        [JsonProperty("guest_product")]
        public GuestProductDto GuestProduct { get; set; }

        [JsonProperty("consumable_products")]
        public IReadOnlyCollection<AdditionalServiceProductConsumableDto> ConsumableProducts { get; init; }

        public string Name => AdditionalServiceNameRu;

        public string NameUkr => AdditionalServiceNameUa;

        public string NameEn => AdditionalServiceNameEn;
    }
}