using System;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceRepairDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("defect")]
        public string Defect { get; set; }

        [JsonProperty("repair_invoice")]
        public string RepairInvoice { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("service_center_conclusion")]
        public string ServiceCenterConclusion { get; set; }

        [JsonProperty("service_invoice_id")]
        public int? ServiceInvoiceId { get; set; }

        [JsonProperty("service_center_id")]
        public int? ServiceCenterId { get; set; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }

        [JsonProperty("service_request_order_id")]
        public int ServiceRequestOrderId { get; set; }

        [JsonProperty("service_request_location_id")]
        public int? ServiceRequestLocationId { get; set; }

        [JsonProperty("service_request_subdivision_id")]
        public int ServiceRequestSubdivisionId { get; set; }

        [JsonProperty("service_request_warehouse_location_id")]
        public int? ServiceRequestWarehouseLocationId { get; set; }

        [JsonProperty("service_request_received_on")]
        public DateTime? ServiceRequestReceivedOn { get; set; }

        [JsonProperty("service_request_purchased_from")]
        public int? ServiceRequestPurchasedFrom { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }
    }
}