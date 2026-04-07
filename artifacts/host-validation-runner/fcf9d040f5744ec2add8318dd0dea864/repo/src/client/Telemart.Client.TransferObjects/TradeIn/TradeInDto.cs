using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("order_id")]
        public int? OrderId { get; init; }

        [JsonProperty("presale_order_id")]
        public int? PresaleOrderId { get; init; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; init; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; init; }

        [JsonProperty("category_id")]
        public int? CategoryId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }

        [JsonProperty("customer_id")]
        public int? CustomerId { get; init; }

        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; init; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; init; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("ready_for_complete")]
        public bool ReadyForComplete { get; init; }

        [JsonProperty("class_id")]
        public int ClassId { get; init; }

        [JsonProperty("customer_class_id")]
        public int CustomerClassId { get; init; }

        [JsonProperty("warranty_id")]
        public int WarrantyId { get; init; }

        [JsonProperty("customer_warranty_id")]
        public int CustomerWarrantyId { get; init; }

        [JsonProperty("pack_id")]
        public int PackId { get; init; }

        [JsonProperty("customer_pack_id")]
        public int CustomerPackId { get; init; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; init; }

        [JsonProperty("cancel_reason_id")]
        public int? CancelReasonId { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("trade_in_segment_id")]
        public int? TradeInSegmentId { get; init; }

        [JsonProperty("trade_in_segment_name")]
        public string TradeInSegmentName { get; init; }

        [JsonProperty("buyout_amount")]
        public decimal BuyoutAmount { get; init; }

        [JsonProperty("real_buyout_amount")]
        public decimal? RealBuyoutAmount { get; init; }

        [JsonProperty("first_name")]
        public string FirstName { get; init; }

        [JsonProperty("last_name")]
        public string LastName { get; init; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; init; }

        [JsonProperty("customer_brand")]
        public string CustomerBrand { get; init; }

        [JsonProperty("brand")]
        public string Brand { get; init; }

        [JsonProperty("model_or_pn")]
        public string ModelOrPn { get; init; }

        [JsonProperty("customer_model_or_pn")]
        public string CustomerModelOrPn { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("customer_comment")]
        public string CustomerComment { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("customer_description")]
        public string CustomerDescription { get; init; }

        [JsonProperty("class_description")]
        public string ClassDescription { get; init; }

        [JsonProperty("class_description_ukr")]
        public string ClassDescriptionUkr { get; init; }

        [JsonProperty("customer_class_description")]
        public string CustomerClassDescription { get; init; }

        [JsonProperty("return_invoice_1c_response")]
        public string ReturnInvoice1cResponse { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("tested")]
        public bool Tested { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("canceled_on")]
        public DateTime? CanceledOn { get; init; }

        [JsonProperty("canceled_by")]
        public int? CanceledBy { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("evaluated_on")]
        public DateTime? EvaluatedOn { get; init; }

        [JsonProperty("evaluated_by")]
        public int? EvaluatedBy { get; init; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; init; }

        [JsonProperty("received_by")]
        public int? ReceivedBy { get; init; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; init; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; init; }

        [JsonProperty("tested_on")]
        public DateTime? TestedOn { get; init; }

        [JsonProperty("tested_by")]
        public int? TestedBy { get; init; }

        [JsonProperty("create_invoice_1c_id")]
        public long? CreateInvoice1cId { get; init; }

        [JsonProperty("create_invoice_1c_response")]
        public string CreateInvoice1cResponse { get; init; }

        [JsonProperty("trade_in_max_price")]
        public decimal? TradeInMaxPrice { get; init; }

        [JsonProperty("last_date_trade_in_max_price")]
        public DateTime? LastDateTradeInMaxPrice { get; init; }

        [JsonProperty("complaints_count")]
        public int? ComplaintsCount { get; set; }

        [JsonProperty("new_complaints_count")]
        public int? NewComplaintsCount { get; set; }

        [JsonProperty("carry_in_id")]
        public int? CarryInId { get; init; }

        [JsonProperty("carry_out_id")]
        public int? CarryOutId { get; init; }

        [JsonProperty("city_out_id")]
        public int? CityOutId { get; init; }

        [JsonProperty("delivery_data_out")]
        public DeliveryDataDto DeliveryDataOut { get; set; }

        [JsonProperty("ttn_out")]
        public string TtnOut { get; init; }

        [JsonProperty("ttn_in")]
        public string TtnIn { get; init; }

        [JsonProperty("inn")]
        public string Inn { get; init; }

        [JsonProperty("military_tax_amount")]
        public decimal? MilitaryTaxAmount { get; init; }

        [JsonProperty("military_tax_value")]
        public decimal? MilitaryTaxValue { get; init; }

        [JsonProperty("pdf_tax_amount")]
        public decimal? PdfTaxAmount { get; init; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; init; }

        [JsonProperty("bought_in_telemart")]
        public bool BoughtInTelemart { get; init; }

        [JsonProperty("has_completed_documents")]
        public bool HasCompletedDocuments { get; init; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}