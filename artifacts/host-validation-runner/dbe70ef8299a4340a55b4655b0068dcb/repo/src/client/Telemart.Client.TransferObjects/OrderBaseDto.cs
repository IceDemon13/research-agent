using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public abstract class OrderBaseDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("delivery_time")]
        public DateTime? DeliveryTime { get; init; }

        [JsonProperty("delivery_time_to")]
        public DateTime? DeliveryTimeTo { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("min_invoice_close_date")]
        public DateTime? MinInvoiceCloseDate { get; init; }

        [JsonProperty("receive_time")]
        public DateTime? ReceiveTime { get; set; }

        [JsonProperty("completed_on_fiscal_registrar")]
        public bool CompletedOnFiscalRegistrar { get; init; }

        [JsonProperty("client_id")]
        public int ClientId { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("pko")]
        public int Pko { get; init; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("buffer_warehouse_id")]
        public int? BufferWarehouseId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("customer_state_text")]
        public string CustomerStateText { get; init; }

        [JsonProperty("package_delivery_cost")]
        public int PackageDeliveryCost { get; init; }

        [JsonProperty("package_ttn")]
        public string PackageTtn { get; init; }

        [JsonProperty("customer_received_on")]
        public DateTime? CustomerReceivedOn { get; init; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; init; }

        [JsonProperty("system_comment")]
        public string SystemComment { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("rt")]
        public int Rt { get; init; }

        [JsonProperty("confirmed_by")]
        public int? ConfirmedBy { get; init; }

        [JsonProperty("state_text")]
        public string StateText { get; init; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; init; }

        [JsonProperty("customer_id")]
        public int? CustomerId { get; init; }

        [JsonProperty("customer_est_id")]
        public int? CustomerEstId { get; init; }

        [JsonProperty("total_cost_usd")]
        public decimal? TotalCostUsd { get; set; }

        [JsonProperty("options")]
        public OrderOptionsDto Options { get; init; }

        [JsonProperty("new_calls_count")]
        public int? NewCallsCount { get; set; }

        [JsonProperty("external_payments")]
        public IReadOnlyCollection<ExternalPaymentDto> ExternalPayments { get; init; }
    }
}