using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Bundle;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class ProductDto : ILocalіzableEntity
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("image_id")]
        public int? ImageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("prefix_ru")]
        public string PrefixRu { get; set; }

        [JsonProperty("prefix_ua")]
        public string PrefixUa { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        [JsonProperty("name_full_ru")]
        public string NameFullRu { get; set; }

        [JsonProperty("name_full_ukr")]
        public string NameFullUa { get; set; }

        [JsonProperty("name_full_en")]
        public string NameFullEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_short")]
        public string DescriptionShort { get; set; }

        [JsonProperty("description_short_ukr")]
        public string DescriptionShortUkr { get; set; }

        [JsonProperty("description_short_en")]
        public string DescriptionShortEn { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("min_leftover")]
        public int? MinLeftover { get; set; }

        [JsonProperty("segment_name")]
        public string SegmentName { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("usd_currency")]
        public int UsdCurrency { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }

        [JsonProperty("weight_est")]
        public double WeightEstimated { get; set; }

        [JsonProperty("label_id")]
        public int? LabelId { get; set; }

        [JsonProperty("label_name")]
        public string LabelName { get; set; }

        [JsonProperty("label_name_short")]
        public string LabelNameShort { get; set; }

        [JsonProperty("label_rewrite")]
        public string LabelRewrite { get; set; }

        [JsonProperty("label_wight")]
        public int? LabelWeight { get; set; }

        [JsonProperty("warranty_off")]
        public bool WarrantyOff { get; set; }

        [JsonProperty("warranty_id")]
        public int WarrantyId { get; set; }

        [JsonProperty("warranty_name")]
        public string WarrantyName { get; set; }

        [JsonProperty("warranty_name_ukr")]
        public string WarrantyNameUkr { get; set; }

        [JsonProperty("warranty_name_en")]
        public string WarrantyNameEn { get; set; }

        [JsonProperty("warranty_name_short")]
        public string WarrantyNameShort { get; set; }

        [JsonProperty("ym_id")]
        public string YmId { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; set; }

        [JsonProperty("trade_in_segment_id")]
        public int? TradeInSegmentId { get; init; }

        [JsonProperty("trade_in_segment_name")]
        public string TradeInSegmentName { get; init; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("modific")]
        public string Modific { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("color_primary")]
        public ProductColorDto ColorPrimary { get; set; }

        [JsonProperty("color_secondary")]
        public ProductColorDto ColorSecondary { get; set; }

        [JsonProperty("pn")]
        public string Pn { get; set; }

        [JsonProperty("link_rewrite")]
        public string LinkRewrite { get; set; }

        [JsonProperty("complect")]
        public string Complect { get; set; }

        [JsonProperty("complect_ukr")]
        public string ComplectUkr { get; set; }

        [JsonProperty("complect_en")]
        public string ComplectEn { get; set; }

        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; set; }

        [JsonProperty("avail_id")]
        public int AvailId { get; set; }

        [JsonProperty("avail_name")]
        public string AvailName { get; set; }

        [JsonProperty("avail_type_id")]
        public int AvailTypeId { get; set; }

        [JsonProperty("avail_type_descr")]
        public string AvailTypeDescr { get; set; }

        [JsonProperty("popular")]
        public int Popular { get; set; }

        [JsonProperty("visits")]
        public int Visits { get; set; }

        [JsonProperty("stars")]
        public decimal Stars { get; set; }

        [JsonProperty("stars_count")]
        public int StarsCount { get; set; }

        [JsonProperty("stars_price")]
        public decimal? StarsPrice { get; set; }

        [JsonProperty("stars_quality")]
        public decimal? StarsQuality { get; set; }

        [JsonProperty("stars_functionality")]
        public decimal? StarsFunctionality { get; set; }

        [JsonProperty("recommended")]
        public int? Recommended { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }

        [JsonProperty("product_mark_top")]
        public string ProductMarkTop { get; set; }

        [JsonProperty("hotline")]
        public bool Hotline { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("catalog_links")]
        public string CatalogLinks { get; set; }

        [JsonProperty("delivery_cost")]
        public int DeliveryCost { get; set; }

        [JsonProperty("tax_rate_id")]
        public int TaxRateId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("assembly_quantity")]
        public int? AssemblyQuantity { get; set; }

        [JsonProperty("price_in_usd")]
        public decimal? PriceInUsd { get; set; }

        [JsonProperty("price_in")]
        public decimal? PriceIn { get; set; }

        [JsonProperty("prices")]
        public IReadOnlyCollection<ProductPriceSimpleDto> Prices { get; set; }

        [JsonProperty("images")]
        public ProductImageDto[] Images { get; set; }

        [JsonProperty("warehouses")]
        public ProductWarehouseDto[] Warehouses { get; set; }

        [JsonProperty("colors")]
        public ProductDto[] Colors { get; set; }

        [JsonProperty("gifts")]
        public ProductDto[] Gifts { get; set; }

        [JsonProperty("promo_codes")]
        public IReadOnlyCollection<ProductPromoCodeDto> PromoCodes { get; set; }

        [JsonProperty("bundles")]
        public IReadOnlyCollection<BundleDto> Bundles { get; set; }

        [JsonProperty("complectation")]
        public ProductDto[] Complectation { get; set; }

        [JsonProperty("additional_service_groups")]
        public ProductAdditionalServiceGroupDto[] AdditionalServiceGroups { get; set; }

        [JsonProperty("additional_service_provide_product_ids")]
        public IReadOnlyCollection<int> AdditionalServiceProvideProductIds { get; set; }

        #region Bonuses

        [JsonProperty("bonus_type_id")]
        public int? BonusTypeId { get; set; }

        [JsonProperty("max_bonuses_to_use")]
        public int? MaxBonusesToUse { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        #endregion

        string ILocalіzableEntity.Name => NameFullRu;

        string ILocalіzableEntity.NameUkr => NameFullUa;

        string ILocalіzableEntity.NameEn => NameFullEn;
    }
}