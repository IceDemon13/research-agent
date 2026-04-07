using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceTemplateDto
    {
        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("date_close")]
        public DateTime DateClose { get; set; }

        [JsonProperty("date_get")]
        public DateTime DateGet { get; set; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("state_id")]
        public int? StateId { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; set; }

        [JsonProperty("supplier_warehouse_name")]
        public string SupplierWarehouseName { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("main")]
        public bool? Main { get; set; }
    }
}