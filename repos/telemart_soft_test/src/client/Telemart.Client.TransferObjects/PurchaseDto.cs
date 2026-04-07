using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchaseDto
    {
        [JsonProperty("invoice_employee_sup_id")]
        public int? InvoiceEmployeeSupId { get; set; }

        [JsonProperty("product_currency_out_id")]
        public int ProductCurrencyOutId { get; set; }

        [JsonProperty("product_employee_sup_id")]
        public int ProductEmployeeSupId { get; set; }

        [JsonProperty("product_employee_id")]
        public int ProductEmployeeId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("invoice_id")]
        public int? ProductInvoiceId { get; set; }

        [JsonProperty("product_movement_id")]
        public int? ProductMovementId { get; set; }

        [JsonProperty("product_position")]
        public int ProductPosition { get; set; }

        [JsonProperty("product_price_1c")]
        public decimal ProductPrice1C { get; set; }

        [JsonProperty("product_price_out")]
        public decimal ProductPriceOut { get; set; }

        [JsonProperty("product_product_id")]
        public int ProductProductId { get; set; }

        [JsonProperty("product_product_name")]
        public string ProductProductName { get; set; }

        [JsonProperty("product_quantity")]
        public int ProductQuantity { get; set; }

        [JsonProperty("order_product_quantity")]
        public int OrderProductQuantity { get; set; }

        [JsonProperty("product_source_date")]
        public DateTime? ProductSourceDate { get; set; }

        [JsonProperty("bonuses_applied")]
        public bool BonusesApplied { get; set; }

        [JsonProperty("product_source_id")]
        public int ProductSourceId { get; set; }

        [JsonProperty("product_source_text")]
        public string ProductSourceText { get; set; }

        [JsonProperty("product_state_id")]
        public int ProductStateId { get; set; }

        [JsonProperty("product_warehouse_id")]
        public int? ProductWarehouseId { get; set; }

        [JsonProperty("product_category_ids")]
        public int[] ProductCategoryIds { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_carry_id")]
        public int OrderCarryId { get; set; }

        [JsonProperty("order_client_id")]
        public int OrderClientId { get; set; }

        [JsonProperty("order_employee_lock_id")]
        public int? OrderEmployeeLockId { get; set; }

        [JsonProperty("order_employee_lock_name")]
        public string OrderEmployeeLockName { get; set; }

        [JsonProperty("order_confirmed_by")]
        public int? OrderConfirmedBy { get; set; }

        [JsonProperty("order_created_by")]
        public int OrderCreatedBy { get; set; }

        [JsonProperty("order_payment_id")]
        public int OrderPaymentId { get; set; }

        [JsonProperty("order_state_id")]
        public int OrderStateId { get; set; }

        [JsonProperty("order_subdivision_id")]
        public int OrderSubdivisionId { get; set; }

        [JsonProperty("order_warehouse_id")]
        public int? OrderWarehouseId { get; set; }

        [JsonProperty("order_service_request_id")]
        public int? OrderBasedOnServiceRequestId { get; set; }

        [JsonProperty("employee_comment")]
        public string EmployeeComment { get; set; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; set; }

        [JsonProperty("system_comment")]
        public string SystemComment { get; set; }

        [JsonProperty("order_receive_time")]
        public DateTime? OrderReceiveTime { get; set; }

        [JsonProperty("order_created_on")]
        public DateTime OrderCreatedOn { get; set; }

        [JsonProperty("order_delivery_time")]
        public DateTime? OrderDeliveryTime { get; set; }

        [JsonProperty("order_delivery_time_to")]
        public DateTime? OrderDeliveryTimeTo { get; set; }

        [JsonProperty("order_pko")]
        public int OrderPko { get; set; }

        [JsonProperty("order_rt")]
        public int OrderRt { get; set; }

        [JsonProperty("order_contains_assembly_service")]
        public bool OrderContainsAssemblyService { get; set; }

        [JsonProperty("invoice_date_close")]
        public DateTime? InvoiceDateClose { get; set; }

        [JsonProperty("order_folder_type_id")]
        public int? OrderFolderTypeId { get; set; }

        [JsonProperty("price_telemart_1")]
        public decimal PriceTelemart1 { get; set; }

        [JsonProperty("order_product_create_on")]
        public DateTime? OrderProductCreateOn { get; set; }
    }
}