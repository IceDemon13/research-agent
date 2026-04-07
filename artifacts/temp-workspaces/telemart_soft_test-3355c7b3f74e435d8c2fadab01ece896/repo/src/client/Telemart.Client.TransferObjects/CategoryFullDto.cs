using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryFullDto : ICloneable
    {
        private ProductMarkingOptionsDto _productMarkingOptions;

        public CategoryFullDto()
        {
            _productMarkingOptions = new ProductMarkingOptionsDto();
        }

        [JsonProperty("left")]
        public int Left { get; set; }

        [JsonProperty("right")]
        public int Right { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("parent_level")]
        public int ParentLevel { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("ratio_update")]
        public DateTime RatioUpdate { get; set; }

        #region Trading Platforms

        [JsonProperty("hotline")]
        public bool Hotline { get; set; }

        #endregion

        #region Main

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }

        [JsonProperty("category_1C")]
        public int Category1C { get; set; }

        [JsonProperty("yandex_market_hid")]
        public int YandexMarketHid { get; set; }

        [JsonProperty("link_rewrite")]
        public string LinkRewrite { get; set; }

        [JsonProperty("link_rewrite_full")]
        public string LinkRewriteFull { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_full")]
        public string NameFull { get; set; }

        [JsonProperty("name_full_ukr")]
        public string NameFullUkr { get; set; }

        [JsonProperty("name_full_en")]
        public string NameFullEn { get; set; }

        [JsonProperty("name_trans")]
        public string NameTrans { get; set; }

        [JsonProperty("name_trans_ukr")]
        public string NameTransUkr { get; set; }

        [JsonProperty("name_trans_en")]
        public string NameTransEn { get; set; }

        [JsonProperty("name_breadcrumbs")]
        public string NameBreadcrumbs { get; set; }

        [JsonProperty("name_breadcrumbs_ukr")]
        public string NameBreadcrumbsUkr { get; set; }

        [JsonProperty("name_breadcrumbs_en")]
        public string NameBreadcrumbsEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_ukr")]
        public string DescriptionUkr { get; set; }

        [JsonProperty("description_en")]
        public string DescriptionEn { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufactor { get; set; }

        [JsonProperty("is_parent")]
        public bool IsParent { get; set; }

        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("promo_info")]
        public string PromoInfo { get; set; }

        [JsonProperty("promo_info_ukr")]
        public string PromoInfoUkr { get; set; }

        [JsonProperty("promo_info_en")]
        public string PromoInfoEn { get; set; }

        [JsonProperty("use_in_trade_in")]
        public bool UseInTradeIn { get; set; }

        [JsonProperty("trade_in_service_cost")]
        public decimal TradeInServiceCost { get; set; }

        [JsonProperty("trade_in_service_cost_currency_id")]
        public int TradeInServiceCostCurrencyId { get; set; }

        [JsonProperty("min_category_margin_percent")]
        public decimal? MinCategoryMarginPercent { get; set; }

        #endregion

        #region Robot

        [JsonProperty("robot_script")]
        public string RobotScript { get; set; }

        [JsonProperty("robot_script_parameters")]
        public string RobotScriptParameters { get; set; }

        [JsonProperty("robot_mode_manual_id")]
        public int RobotModeManualId { get; set; }

        [JsonProperty("robot_mode_auto_id")]
        public int RobotModeAutoId { get; set; }

        #endregion

        #region Properties

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("employee_supplier_id")]
        public int EmployeeSupId { get; set; }

        [JsonProperty("warranty_type_id")]
        public int? WarrantyTypeId { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("usd_currency")]
        public int UsdCurrency { get; set; }

        [JsonProperty("id_warranty_retail")]
        public int WarrantyRetailId { get; set; }

        [JsonProperty("id_warranty_wholesale")]
        public int WarrantyWholesaleId { get; set; }

        [JsonProperty("tag_format_id")]
        public int TagFormatId { get; set; }

        [JsonProperty("tax_rate_id")]
        public int TaxRateId { get; set; }

        [JsonProperty("type_id")]
        public int? TypeId { get; set; }

        [JsonProperty("admitad_tariff_code_id")]
        public int? AdmitadTariffCodeId { get; set; }

        [JsonProperty("sales_doubler_tariff_code_id")]
        public int? SalesDoublerTariffCodeId { get; set; }

        [JsonProperty("referral_discount_code_id")]
        public int? ReferralDiscountCodeId { get; set; }

        [JsonProperty("segment_limit")]
        public int? SegmentLimit { get; set; }

        [JsonProperty("segment_features_limit")]
        public int? SegmentFeaturesLimit { get; set; }

        [JsonProperty("short_names_for_1c")]
        public bool ShortNamesFor1C { get; set; }

        [JsonProperty("planned_markup")]
        public decimal PlannedMarkup { get; set; }

        [JsonProperty("use_new_robot")]
        public bool UseNewRobot { get; set; }

        [JsonProperty("use_planned_markup_in_autoshowcase")]
        public bool UsePlannedMarkupInAutoShowcase { get; set; }

        [JsonProperty("showcase_sku_limit")]
        public int ShowcaseSkuLimit { get; set; }

        #endregion

        #region Logistics

        [JsonProperty("print_warranty_card")]
        public bool PrintWarrantyCard { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("keep_pn")]
        public bool KeepPn { get; set; }

        [JsonProperty("weight_est")]
        public double WeightEstimated { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }

        [JsonProperty("self_barcode")]
        public bool SelfBarcode { get; set; }

        [JsonProperty("keep_dimensions")]
        public bool KeepDimensions { get; set; }

        [JsonProperty("sticker_fragile")]
        public bool StickerFragile { get; set; }

        [JsonProperty("sticker_this_way_up")]
        public bool StickerThisWayUp { get; set; }

        #endregion

        #region Content

        [JsonProperty("days_to_fill")]
        public int DaysToFill { get; set; }

        [JsonProperty("need_content")]
        public bool NeedContent { get; set; }

        [JsonProperty("need_video")]
        public bool NeedVideo { get; set; }

        [JsonProperty("need_complect")]
        public bool NeedComplect { get; set; }

        [JsonProperty("need_description")]
        public int NeedDescription { get; set; }

        [JsonProperty("need_photo")]
        public int NeedPhoto { get; set; }

        #endregion

        #region Prefix

        [JsonProperty("prefix_rus")]
        public string PrefixRus { get; set; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        #endregion

        #region Mask

        [JsonProperty("fmask_t")]
        public string FmaskT { get; set; }

        [JsonProperty("fmask_t_ua")]
        public string FmaskTUa { get; set; }

        [JsonProperty("fmask_t_en")]
        public string FmaskTEn { get; set; }

        #endregion

        #region KPI

        [JsonProperty("category_priority")]
        public int? CategoryPriority { get; set; }

        [JsonProperty("planned_turnover")]
        public decimal? PlannedTurnover { get; set; }

        [JsonProperty("planned_gross_profit")]
        public decimal? PlannedGrossProfit { get; set; }

        [JsonProperty("planned_profit")]
        public decimal? PlannedProfit { get; set; }

        [JsonProperty("planned_quantity")]
        public decimal? PlannedQuantity { get; set; }

        [JsonProperty("purchase_perscent")]
        public decimal? PurchasePercent { get; set; }

        [JsonProperty("bounty_percent")]
        public decimal? BountyPercent { get; set; }

        #endregion

        #region PrintMarkering

        [JsonProperty("print_markers")]
        public bool PrintMarkers { get; set; }

        [JsonProperty("marker_manufacture")]
        public string MarkerManufacture { get; set; }

        [JsonProperty("marker_manufacture_address")]
        public string MarkerManufactureAddress { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        #endregion

        object ICloneable.Clone()
        {
            return Clone();
        }

        public CategoryFullDto Clone()
        {
            CategoryFullDto categoryFullNew = (CategoryFullDto)MemberwiseClone();
            categoryFullNew._productMarkingOptions = _productMarkingOptions.Clone();
            return categoryFullNew;
        }
    }
}