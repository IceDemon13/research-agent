using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Promo;

namespace Telemart.Client.TransferObjects.Prices
{
    public class ProductPriceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("pn")]
        public string Pn { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("avail_id")]
        public int AvailId { get; set; }

        [JsonProperty("F2_markup")]
        public double F2Markup { get; set; }

        [JsonProperty("visits")]
        public int Visits { get; set; }

        [JsonProperty("in_stock")]
        public int InStock { get; set; }

        [JsonProperty("showcases_stock")]
        public int ShowcasesStock { get; set; }

        [JsonProperty("showcases_capacity")]
        public int ShowcasesCapacity { get; set; }

        [JsonProperty("days_in_stock")]
        public int? DaysInStock { get; set; }

        [JsonProperty("sales_current_month")]
        public int SalesCurrentMonth { get; set; }

        [JsonProperty("contractor_allow_document")]
        public bool? ContractorAllowDocument { get; set; }

        [JsonProperty("sales_last_month")]
        public int SalesLastMonth { get; set; }

        [JsonProperty("sales_before_last_month")]
        public int SalesBeforeLastMonth { get; set; }

        [JsonProperty("orders_current_month")]
        public int OrdersCurrentMonth { get; set; }

        [JsonProperty("orders_last_month")]
        public int OrdersLastMonth { get; set; }

        [JsonProperty("orders_before_last_month")]
        public int OrdersBeforeLastMonth { get; set; }

        [JsonProperty("conversion")]
        public double Conversion { get; set; }

        [JsonProperty("days_from_last_sale")]
        public int DaysFromLastSale { get; set; }

        [JsonProperty("last_sale_price_usd")]
        public decimal LastSalePriceUsd { get; set; }

        [JsonProperty("price_warehouse_usd")]
        public decimal? PriceWarehouseUsd { get; set; }

        [JsonProperty("price_in_usd")]
        public decimal? PriceInUsd { get; set; }

        [JsonProperty("price_competitor_usd")]
        public decimal? PriceCompetitorUsd { get; set; }

        [JsonProperty("price_transit_usd")]
        public decimal? PriceTransitUsd { get; set; }

        [JsonProperty("label_retail_id")]
        public int? LabelRetailId { get; set; }

        [JsonProperty("label_wholesale_id")]
        public int? LabelWholesaleId { get; set; }

        [JsonProperty("prices")]
        public IReadOnlyCollection<ProductPriceSimpleDto> Prices { get; set; }

        [JsonProperty("price_comment")]
        public string PriceComment { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("usd_currency")]
        public int UsdCurrency { get; set; }

        [JsonProperty("robot_mode_manual_id")]
        public int RobotModeManualId { get; set; }

        [JsonProperty("robot_mode_auto_id")]
        public int RobotModeAutoId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_robot_mode_manual_id")]
        public int CategoryRobotModeManualId { get; set; }

        [JsonProperty("category_robot_mode_auto_id")]
        public int CategoryRobotModeAutoId { get; set; }

        [JsonProperty("hotline")]
        public int Hotline { get; set; }

        [JsonProperty("hotline_position")]
        public int? HotlinePosition { get; set; }

        [JsonProperty("hotline_min_price")]
        public decimal? HotlineMinPriceUsd { get; set; }

        [JsonProperty("hotline_price")]
        public decimal? HotlinePriceUsd { get; set; }

        [JsonProperty("ratio")]
        public int Ratio { get; set; }

        [JsonProperty("min_leftover")]
        public int? MinLeftover { get; set; }

        [JsonProperty("planned_leftover")]
        public int PlannedLeftover { get; set; }

        [JsonProperty("rozetka")]
        public int Rozetka { get; set; }

        [JsonProperty("monomarket")]
        public int Monomarket { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }

        [JsonProperty("show_in_accessories")]
        public bool ShowInAccessories { get; set; }

        [JsonProperty("bonus_type_id")]
        public int? BonusTypeId { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        [JsonProperty("warranty_retail")]
        public double WarrantyRetail { get; set; }

        [JsonProperty("based_on_prices")]
        public IReadOnlyCollection<ProductPriceSimpleDto> BasedOnPrices { get; set; }

        [JsonProperty("assembled_computer_rule_base_prices")]
        public IReadOnlyCollection<ProductPriceSimpleDto> AssembledComputerRuleBasePrices { get; set; }

        [JsonProperty("max_trade_in_price")]
        public double? MaxTradeInPrice { get; set; }

        [JsonProperty("mining")]
        public bool Mining { get; set; }

        [JsonProperty("segment_name")]
        public string SegmentName { get; set; }

        [JsonProperty("segment_id")]
        public int SegmentId { get; set; }

        [JsonProperty("segment_price_out_abc_id")]
        public int? SegmentPriceOutAbcId { get; set; }

        [JsonProperty("segment_profit_abc_id")]
        public int? SegmentProfitAbcId { get; set; }

        [JsonProperty("segment_order_quantity_abc_id")]
        public int? SegmentOrderQuantityAbcId { get; set; }

        [JsonProperty("product_category_profit_abc_id")]
        public int? ProductCategoryProfitAbcId { get; set; }

        [JsonProperty("product_category_order_quantity_abc_id")]
        public int? ProductCategoryOrderQuantityAbcId { get; set; }

        [JsonProperty("product_category_category_price_out_abc_id")]
        public int? ProductCategoryPriceOutAbcId { get; set; }

        [JsonProperty("segment_category_price_out_abc_id")]
        public int? SegmentCategoryPriceOutAbcId { get; set; }

        [JsonProperty("segment_category_profit_abc_id")]
        public int? SegmentCategoryProfitAbcId { get; set; }

        [JsonProperty("segment_category_order_quantity_abc_id")]
        public int? SegmentCategoryOrderQuantityAbcId { get; init; }

        [JsonProperty("promo")]
        public PromoSimpleDto ProductPromo { get; init; }

        [JsonProperty("competitor_prices")]
        public IReadOnlyCollection<ProductContractorPriceDto> CompetitorPrices { get; init; }

        [JsonProperty("supplier_prices")]
        public IReadOnlyCollection<ProductContractorPriceDto> SupplierPrices { get; init; }

        [JsonProperty("rrp_prices")]
        public IReadOnlyCollection<ProductContractorPriceDto> RrpPrices { get; init; }

        [JsonProperty("configurator_prices")]
        public IReadOnlyCollection<ProductContractorPriceDto> ConfiguratorPrices { get; init; }

        [JsonProperty("hotline_abc_class_prices")]
        public IReadOnlyCollection<AbcClassPriceDto> HotlineAbcClassPrices { get; init; }

        [JsonProperty("sales")]
        public IReadOnlyCollection<ProductSalesDto> Sales { get; init; }

        [JsonProperty("purchases")]
        public IReadOnlyCollection<ProductPurchasesDto> Purchases { get; init; }

        [JsonProperty("promo_history")]
        public IReadOnlyCollection<PromoHistoryDto> PromoHistory { get; init; }

        [JsonProperty("search_template_prices")]
        public IReadOnlyCollection<ProductSearchTemplatePriceDto> SearchTemplatePrices { get; init; }

        [JsonProperty("storage_in_stock")]
        public int StorageInStock { get; init; }

        [JsonProperty("transit_in_stock")]
        public int TransitInStock { get; init; }

        [JsonProperty("showcase_pickup_mode_id")]
        public int? ShowcasePickupModeId { get; init; }

        [JsonProperty("reserved_quantity")]
        public int ReservedQuantity { get; init; }

        [JsonProperty("density_14_days")]
        public double Density14Days { get; init; }

        [JsonProperty("density_1_month")]
        public double Density1Month { get; init; }

        [JsonProperty("planned_markup")]
        public double PlannedMarkup { get; init; }

        [JsonProperty("use_planned_markup_in_autoshowcase")]
        public bool UsePlannedMarkupInAutoShowcase { get; init; }

        [JsonProperty("not_saved_price1_usd")]
        public double? NotSavedPrice1Usd { get; init; }

        [JsonProperty("main_stock")]
        public int MainStock { get; init; }

        [JsonProperty("product_type_id")]
        public int? ProductTypeId { get; init; }

        [JsonProperty("preorder")]
        public bool Preorder { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("name_en")]
        public string NameEn { get; init; }

        [JsonProperty("avail_modified_by")]
        public int AvailModifiedBy { get; init; }

        [JsonProperty("min_product_margin_percent")]
        public decimal MinProductMarginPercent { get; init; }

        [JsonProperty("min_category_margin_percent")]
        public decimal? MinCategoryMarginPercent { get; init; }
    }
}