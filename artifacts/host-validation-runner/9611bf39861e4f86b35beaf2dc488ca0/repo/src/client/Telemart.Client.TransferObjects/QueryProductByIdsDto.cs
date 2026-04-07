using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class QueryProductByIdsDto
    {
        public QueryProductByIdsDto(
            int[] productIds,
            int contractorId,
            bool queryGifts = false,
            bool queryAdditionalServices = false,
            bool priceIn = false,
            bool priceInIncludeReserve = false,
            bool includePrices = false,
            bool? priceQuantity = false,
            bool? additionalServiceProvideProducts = false,
            int[] cartProductIds = null,
            bool? conditionGifts = null)
        {
            Gifts = queryGifts;
            AdditionalServices = queryAdditionalServices;
            PriceIn = priceIn;
            PriceInIncludeReserve = priceInIncludeReserve;
            ProductIds = productIds;
            ContractorId = contractorId;
            IncludePrices = includePrices;
            PriceQuantity = priceQuantity;
            AdditionalServiceProvideProducts = additionalServiceProvideProducts;
            CartProductIds = cartProductIds;
            ConditionGifts = conditionGifts;
        }

        [JsonProperty("ids")]
        public int[] ProductIds { get; init; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("colors")]
        public bool? Colors { get; init; }

        [JsonProperty("images")]
        public bool? Images { get; init; }

        [JsonProperty("warehouses")]
        public bool? Warehouses { get; init; }

        [JsonProperty("videos")]
        public bool? Videos { get; init; }

        [JsonProperty("reviews")]
        public bool? Reviews { get; init; }

        [JsonProperty("gifts")]
        public bool? Gifts { get; init; }

        [JsonProperty("promo_codes")]
        public bool? PromoCodes { get; init; }

        [JsonProperty("promos")]
        public bool? Promos { get; init; }

        [JsonProperty("bundles")]
        public bool? Bundles { get; init; }

        [JsonProperty("features")]
        public bool? Features { get; init; }

        [JsonProperty("accessories")]
        public bool? Accessories { get; init; }

        [JsonProperty("complectation")]
        public bool? Complectation { get; init; }

        [JsonProperty("additional_services")]
        public bool? AdditionalServices { get; init; }

        [JsonProperty("sort_by_ids")]
        public bool? SortByIds { get; init; }

        [JsonProperty("generate_complectation")]
        public bool? GenerateComplectation { get; init; }

        [JsonProperty("price_in")]
        public bool? PriceIn { get; init; }

        [JsonProperty("price_quantity")]
        public bool? PriceQuantity { get; init; }

        [JsonProperty("price_in_include_reserve")]
        public bool? PriceInIncludeReserve { get; init; }

        [JsonProperty("max_complectation_price")]
        public decimal? MaxComplectationPrice { get; init; }

        [JsonProperty("include_prices")]
        public bool IncludePrices { get; init; }

        [JsonProperty("use_transits")]
        public bool? UseTransits { get; init; }

        [JsonProperty("use_purchases")]
        public bool? UsePurchases { get; init; }

        [JsonProperty("use_warehouse")]
        public bool? UseWarehouse { get; init; }

        [JsonProperty("additional_service_provide_products")]
        public bool? AdditionalServiceProvideProducts { get; init; }

        [JsonProperty("cart_product_ids")]
        public int[] CartProductIds { get; init; }

        [JsonProperty("condition_gifts")]
        public bool? ConditionGifts { get; init; }
    }
}