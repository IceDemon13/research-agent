using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; init; }

        [JsonProperty("bonus_charged")]
        public bool BonusCharged { get; init; }

        [JsonProperty("fio")]
        public string Fio { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("warranty_removed")]
        public bool WarrantyRemoved { get; init; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("product_in_id")]
        public int ProductInId { get; init; }

        [JsonProperty("new_assembled_computer_active")]
        public bool? NewAssembledComputerActive { get; init; }

        [JsonProperty("service_repair_type_id")]
        public int? ServiceRepairTypeId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; init; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; init; }

        [JsonProperty("product_prefix_ru")]
        public string ProductPrefixRus { get; init; }

        [JsonProperty("product_prefix_ukr")]
        public string ProductPrefixUkr { get; init; }

        [JsonProperty("product_prefix_en")]
        public string ProductPrefixEn { get; init; }

        [JsonProperty("product_keep_serial")]
        public bool ProductKeepSerial { get; init; }

        [JsonProperty("product_serial_number_length")]
        public List<ProductSnLengthDto> ProductSerialNumberLength { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("order_payment_type_id")]
        public int OrderPaymentTypeId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("trade_in_id")]
        public int? TradeInId { get; init; }

        [JsonProperty("customer_state_text")]
        public string CustomerStateText { get; init; }

        [JsonProperty("requirement_id")]
        public int Requirement { get; init; }

        [JsonProperty("group")]
        public ServiceRequestGroupDto Group { get; init; }

        [JsonProperty("bundle_id")]
        public int? BundleId { get; init; }

        [JsonProperty("requirement_resolution_id")]
        public int? RequirementResolution { get; init; }

        [JsonProperty("requirement_payment_id")]
        public int? RequirementPaymentId { get; init; }

        [JsonProperty("requirement_text")]
        public string RequirementText { get; init; }

        [JsonProperty("requirement_resolution_text")]
        public string RequirementResolutionText { get; init; }

        [JsonProperty("sn")]
        public string SerialNumber { get; init; }

        [JsonProperty("phone2")]
        public string Phone2 { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }

        [JsonProperty("stated_defect")]
        public string StatedDefect { get; init; }

        [JsonProperty("apppearance")]
        public string Appearance { get; init; }

        [JsonProperty("inspection")]
        public string Inspection { get; init; }

        [JsonProperty("reject_reason_id")]
        public int? RejectReasonId { get; init; }

        [JsonProperty("completeness_id")]
        public int CompletenessId { get; init; }

        [JsonProperty("completeness_comment")]
        public string CompletenessComment { get; init; }

        [JsonProperty("exchange_on")]
        public string ExchangeOn { get; init; }

        [JsonProperty("exchange_fund")]
        public string ExchangeFund { get; init; }

        [JsonProperty("source_id")]
        public int SourceId { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("moved_to")]
        public string MovedTo { get; init; }

        [JsonProperty("returned")]
        public string Returned { get; init; }

        [JsonProperty("refund")]
        public string Refund { get; init; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("invoice_id")]
        public int? InvoiceId { get; init; }

        [JsonProperty("service_product_ids")]
        public int[] ServiceProductIds { get; init; }

        [JsonProperty("purchased_from_id")]
        public int? PurchasedFromContractorId { get; init; }

        [JsonProperty("purchased_on")]
        public DateTime? PurchasedOn { get; init; }

        [JsonProperty("product_new_id")]
        public int? ProductNewId { get; init; }

        [JsonProperty("order_new_id")]
        public int? OrderNewId { get; init; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("warehouse_in_id")]
        public int? WarehouseInId { get; init; }

        [JsonProperty("carry_in_id")]
        public int? CarryInId { get; init; }

        [JsonProperty("carry_out_id")]
        public int? CarryOutId { get; set; }

        [JsonProperty("delivery_data_out")]
        public DeliveryDataDto DeliveryDataOut { get; set; }

        [JsonProperty("ttn_in")]
        public string TtnIn { get; init; }

        [JsonProperty("send_to")]
        public string SendTo { get; init; }

        [JsonProperty("ttn_out")]
        public string TtnOut { get; init; }

        [JsonProperty("location")]
        public int? Location { get; init; }

        [JsonProperty("location_text")]
        public string LocationText { get; init; }

        [JsonProperty("warehouse_location_id")]
        public int? WarehouseLocationId { get; init; }

        [JsonProperty("service_repair_id")]
        public int? ServiceRepairId { get; init; }

        [JsonProperty("service_act_number")]
        public string ServiceActNumber { get; init; }

        [JsonProperty("discussion_state")]
        public int DiscussionState { get; init; }

        [JsonProperty("refund_ids")]
        public IReadOnlyCollection<int> RefundIds { get; init; }

        [JsonProperty("customer_id")]
        public int? CustomerId { get; init; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; init; }

        [JsonProperty("received_by")]
        public int? ReceivedBy { get; init; }

        [JsonProperty("ready_on")]
        public DateTime? ReadyOn { get; init; }

        [JsonProperty("diagnostic_on")]
        public DateTime? DiagnosticOn { get; init; }

        [JsonProperty("diagnostic_by")]
        public int? DiagnosticBy { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; init; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; init; }

        [JsonProperty("last_activity_on")]
        public DateTime LastActivityOn { get; init; }

        [JsonProperty("repair_days")]
        public int? RepairDays { get; init; }

        [JsonProperty("date_x")]
        public DateTime? DateX { get; init; }

        [JsonProperty("order_completed_on")]
        public DateTime OrderCompletedOn { get; init; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }

        //// Not mapped to columns

        [JsonProperty("calls_count")]
        public int? CallsCount { get; init; }

        [JsonProperty("new_calls_count")]
        public int? NewCallsCount { get; init; }

        [JsonProperty("discussions_count")]
        public int? DiscussionsCount { get; init; }

        [JsonProperty("documents_count")]
        public int? DocumentsCount { get; init; }

        [JsonProperty("complaints_count")]
        public int? ComplaintsCount { get; init; }

        [JsonProperty("new_complaints_count")]
        public int? NewComplaintsCount { get; init; }

        [JsonProperty("purchased_price")]
        public decimal? PurchasedPrice { get; init; }

        [JsonProperty("purchased_currency_id")]
        public int? PurchasedCurrencyId { get; init; }

        [JsonProperty("trade_in_buyout_amount")]
        public decimal? TradeInBuyoutAmount { get; init; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; init; }

        [JsonProperty("completed_on_fiscal_registrar")]
        public bool CompletedOnFiscalRegistrar { get; init; }

        [JsonProperty("completed_on_money_refund")]
        public bool CompletedOnMoneyRefund { get; init; }

        [JsonProperty("requisites")]
        public RefundRequisitesDto Requisites { get; init; }

        public string ProductFullNameUkr => string.IsNullOrWhiteSpace(ProductPrefixUkr)
            ? ProductName
            : $"{ProductPrefixUkr} {ProductName}";
    }
}