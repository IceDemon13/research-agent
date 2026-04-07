using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("supplier_telegram_chat_id")]
        public string SupplierTelegramChatId { get; set; }

        [JsonProperty("supplier_name")]
        public string SupplierName { get; set; }

        [JsonProperty("supplier_organization")]
        public string SupplierOrganization { get; set; }

        [JsonProperty("supplier_allow_documents")]
        public bool SupplierAllowDocuments { get; set; }

        [JsonProperty("supplier_currency_manual")]
        public bool SupplierCurrencyManual { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; set; }

        [JsonProperty("warehouse_city_name")]
        public string WarehouseCityName { get; set; }

        [JsonProperty("warehouse_address")]
        public string WarehouseAddress { get; set; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; set; }

        [JsonProperty("supplier_warehouse_name")]
        public string SupplierWarehouseName { get; set; }

        [JsonProperty("supplier_auto_reserve")]
        public bool? SupplierAutoReserve { get; set; }

        [JsonProperty("supplier_auto_purchase")]
        public bool? SupplierAutoPurchase { get; set; }

        [JsonProperty("ignore_transit")]
        public bool IgnoreTransit { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("date_get")]
        public DateTime DateGet { get; set; }

        [JsonProperty("date_close")]
        public DateTime DateClose { get; set; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; set; }

        [JsonProperty("received_by")]
        public int? ReceivedBy { get; set; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; set; }

        [JsonProperty("arrived_on")]
        public DateTime? ArrivedOn { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_carrier_id")]
        public int? EmployeeCarrierId { get; set; }

        [JsonProperty("source_current_date_x")]
        public bool? SourceCurrentDateX { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("employee_carrier")]
        public EmployeeSimpleDto EmployeeCarrier { get; set; }

        [JsonProperty("invoice_products")]
        public ICollection<InvoiceProductDto> InvoiceProducts { get; set; }

        [JsonProperty("currency_rates")]
        public ICollection<InvoiceCurrencyRateDto> CurrencyRates { get; set; }

        [JsonProperty("additional_costs")]
        public ICollection<InvoiceAdditionalCostDto> AdditionalCosts { get; set; }

        [JsonProperty("invoice_ttns")]
        public InvoiceTtnDto[] InvoiceTtns { get; set; }
    }
}