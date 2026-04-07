using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchaseInvoiceSourceDto
    {
        [JsonProperty("invoice_id")]
        public int? InvoiceId { get; set; }

        [JsonProperty("invoice_state_id")]
        public int? InvoiceStateId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; set; }

        [JsonProperty("supplier_warehouse_name")]
        public string SupplierWarehouseName { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("date_close")]
        public DateTime DateClose { get; set; }

        [JsonProperty("date_get")]
        public DateTime DateGet { get; set; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("target_warehouse_id")]
        public int? TargetWarehouseId { get; set; }

        [JsonProperty("date_delivery")]
        public DateTime? DeliveryDateTime { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("available_quantity")]
        public int? AvailableQuantity { get; set; }
    }
}