using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceProduct
{
    public class ServiceProductDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; init; }

        [JsonProperty("purchased_price")]
        public decimal? PurchasedPrice { get; init; }

        [JsonProperty("purchased_currency_id")]
        public int? PurchasedCurrencyId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("product_full_name")]
        public string ProductFullName { get; init; }

        [JsonProperty("product_full_name_ukr")]
        public string ProductFullNameUkr { get; init; }

        [JsonProperty("product_full_name_en")]
        public string ProductFullNameEn { get; init; }

        [JsonProperty("sn")]
        public string Sn { get; init; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("loss_usd")]
        public decimal? LossUsd { get; init; }

        [JsonProperty("loss_set_on")]
        public DateTime? LossSetOn { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("product_discount_id")]
        public int? ProductDiscountId { get; init; }

        [JsonProperty("product_discount_name")]
        public string ProductDiscountName { get; init; }

        [JsonProperty("product_discount_name_ukr")]
        public string ProductDiscountNameUkr { get; init; }

        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("service_act_number")]
        public string ServiceActNumber { get; init; }

        [JsonProperty("document_1c_in")]
        public string Document1CIn { get; init; }

        [JsonProperty("document_1c_out")]
        public string Document1COut { get; init; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; init; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; init; }

        [JsonProperty("last_service_repair_id")]
        public int? LastServiceRepairId { get; init; }

        [JsonProperty("warehouse_to_id")]
        public int? WarehouseToId { get; init; }
    }
}