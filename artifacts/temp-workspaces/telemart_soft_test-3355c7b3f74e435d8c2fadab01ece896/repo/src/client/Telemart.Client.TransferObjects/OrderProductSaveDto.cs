using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("currency_out_id")]
        public int CurrencyOutId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("price_out")]
        public decimal PriceOut { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("price_id")]
        public int? PriceId { get; set; }

        [JsonProperty("parent_record_id")]
        public int? ParentRecordId { get; set; }

        [JsonProperty("price_1c")]
        public decimal Price1C { get; set; }

        [JsonProperty("order_promo_code_id")]
        public int? OrderPromoCodeId { get; set; }

        [JsonProperty("promo_discount")]
        public decimal PromoDiscount { get; set; }

        [JsonProperty("assembly_quantity")]
        public int? AssemblyQuantity { get; set; }

        [JsonProperty("assembly_id")]
        public int? AssemblyId { get; set; }

        [JsonProperty("order_folder_id")]
        public int? OrderFolderId { get; set; }

        [JsonProperty("additional_warranty")]
        public bool AdditionalWarranty { get; set; }

        [JsonProperty("assembly_included")]
        public bool AssemblyIncluded { get; set; }

        [JsonProperty("is_gift")]
        public bool IsGift { get; set; }

        [JsonProperty("is_additional_service")]
        public bool IsAdditionalService { get; set; }

        [JsonProperty("is_additional_service_consumable")]
        public bool IsAdditionalServiceConsumable { get; set; }

        [JsonProperty("initiated_by")]
        public int? InitiatedBy { get; set; }

        [JsonProperty("max_bonuses_to_use")]
        public int? MaxBonusesToUse { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }
    }
}