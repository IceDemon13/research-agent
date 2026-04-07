using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_sup_id")]
        public int EmployeeSupId { get; set; }

        [JsonProperty("price_telemart_usd")]
        public decimal PriceTelemartUsd { get; set; }

        [JsonProperty("price_telemart_uah")]
        public decimal PriceTelemartUah { get; set; }

        [JsonProperty("price_comment")]
        public string PriceComment { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("suppliers")]
        public IReadOnlyCollection<ProductInfoContractorPriceDto> Suppliers { get; set; }

        [JsonProperty("competitors")]
        public IReadOnlyCollection<ProductInfoContractorPriceDto> Competitors { get; set; }

        [JsonProperty("rrp")]
        public IReadOnlyCollection<ProductInfoContractorPriceDto> Rrp { get; set; }

        [JsonProperty("leftovers")]
        public IReadOnlyCollection<ProductLeftoversDto> Leftovers { get; set; }

        [JsonProperty("white_stocks")]
        public IReadOnlyCollection<ProductInfoWhiteStockDto> WhiteStocks { get; set; }

        [JsonProperty("purchases")]
        public IReadOnlyCollection<ProductPurchaseHistoryItemDto> Purchases { get; set; }

        [JsonProperty("contractor_sales_history")]
        public IReadOnlyCollection<ProductSalesHistoryItemDto> ContractorSalesHistory { get; set; }

        [JsonProperty("telemart_sales_history")]
        public IReadOnlyCollection<ProductSalesHistoryItemDto> TelemartSalesHistory { get; set; }

        [JsonProperty("purchase_price_stats")]
        public IReadOnlyCollection<ProductInfoPurchasePriceStatsDto> PurchasePriceStats { get; set; }

        [JsonProperty("transits")]
        public IReadOnlyCollection<ProductTransitDto> Transits { get; set; }

        [JsonProperty("movements")]
        public IReadOnlyCollection<ProductInfoMovementDto> Movements { get; set; }

        [JsonProperty("prices")]
        public IReadOnlyCollection<ProductInfoPriceDto> Prices { get; set; }

        [JsonProperty("showcases")]
        public IReadOnlyCollection<ProductInfoShowcaseDto> Showcases { get; set; }

        [JsonProperty("search_templates")]
        public IReadOnlyCollection<ProductInfoSearchTemplateDto> SearchTemplates { get; set; }

        [JsonProperty("hotline")]
        public ProductInfoHotlineDto Hotline { get; set; }

        [JsonProperty("last_contractor_sale")]
        public ProductInfoContractorSalesHistoryDto LastContractorSale { get; set; }

        [JsonProperty("last_telemart_sale")]
        public ProductInfoContractorSalesHistoryDto LastTelemartSale { get; set; }
    }
}