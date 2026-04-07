using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderDto : OrderBaseDto
    {
        [JsonProperty("manager_employee_id")]
        public int ManagerEmployeeId { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("work_place_id")]
        public int? WorkPlaceId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("confirmed_on")]
        public DateTime? ConfirmedOn { get; set; }

        [JsonProperty("employee_pack_id")]
        public int? EmployeePackId { get; set; }

        [JsonProperty("state_change_reason_id")]
        public int? OrderStateChangeReasonId { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }

        [JsonProperty("legal_entity")]
        public LegalEntityDto LegalEntity { get; set; }

        [JsonProperty("old_client")]
        public bool OldClient { get; set; }

        [JsonProperty("client_price_type_id")]
        public int ClientPriceTypeId { get; set; }

        [JsonProperty("order_state_change_comment")]
        public string OrderStateChangeComment { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        [JsonProperty("package_delivery_paid")]
        public int PackageDeliveryPaid { get; set; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; set; }

        [JsonProperty("package_weight")]
        public double PackageWeight { get; set; }

        [JsonProperty("based_on_service_request_id")]
        public int? BasedOnServiceRequestId { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }

        [JsonProperty("assembly_warehouse_id")]
        public int? AssemblyWarehouseId { get; init; }

        [JsonProperty("additional_service_warehouse_id")]
        public int? AdditionalServiceWarehouseId { get; set; }

        [JsonProperty("ready_for_packing")]
        public bool ReadyForPacking { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<OrderProductDto> Products { get; set; }

        [JsonProperty("folders")]
        public IReadOnlyCollection<OrderFolderDto> Folders { get; set; }

        [JsonProperty("promo_codes")]
        public IReadOnlyCollection<OrderPromoCodeDto> PromoCodes { get; set; }

        [JsonProperty("payments")]
        public IReadOnlyCollection<OrderPaymentDto> OrderPayments { get; set; }

        [JsonProperty("bonuses")]
        public IReadOnlyCollection<OrderProductBonusDto> Bonuses { get; set; }

        [JsonProperty("service_requests_count")]
        public int? ServiceRequestsCount { get; set; }

        [JsonProperty("calls_count")]
        public int? CallsCount { get; set; }

        [JsonProperty("complaints_count")]
        public int? ComplaintsCount { get; set; }

        [JsonProperty("new_complaints_count")]
        public int? NewComplaintsCount { get; set; }

        [JsonProperty("order_source_id")]
        public int? OrderSourceId { get; set; }

        [JsonProperty("external_order_id")]
        public string? ExternalOrderId { get; set; }

        [JsonProperty("afterpayment_amount")]
        public decimal AfterpaymentAmount { get; set; }

        [JsonProperty("ignore_delivery_cost_calculation")]
        public bool IgnoreDeliveryCostCalculation { get; init; }

        [JsonProperty("is_collected")]
        public bool? IsCollected { get; set; }

        [JsonProperty("pack_list_id")]
        public int? PackListId { get; set; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; init; }

        [JsonProperty("courier_employee_id")]
        public int? CourierEmployeeId { get; init; }

        [JsonProperty("canceled_from_site")]
        public bool CanceledFromSite { get; init; }

        [JsonProperty("event_unpack_and_cancel_order")]
        public bool EventUnpackAndCancelOrder { get; init; }

        [JsonProperty("money_back_amount")]
        public decimal? MoneyBackAmount { get; init; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}