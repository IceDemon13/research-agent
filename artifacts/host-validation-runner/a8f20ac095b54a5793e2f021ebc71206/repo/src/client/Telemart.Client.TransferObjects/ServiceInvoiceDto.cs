using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceInvoiceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("service_center_id")]
        public int ServiceCenterId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("employee_carrier_id")]
        public int? EmployeeCarrierId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("ttn")]
        public string Ttn { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by_id")]
        public int CreatedById { get; set; }

        [JsonProperty("send_date")]
        public DateTime? SendDate { get; set; }

        [JsonProperty("sent_on")]
        public DateTime? SentOn { get; set; }

        [JsonProperty("products")]
        public ServiceInvoiceProductDto[] Products { get; set; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}