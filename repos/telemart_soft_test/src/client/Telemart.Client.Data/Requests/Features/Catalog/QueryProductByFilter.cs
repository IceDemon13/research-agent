using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductByFilter : CallActionWithBodyRequestBase<PagedResult<ProductDto>,
        QueryProductByFilter.QueryProductByFilterRequest>
    {
        public IReadOnlyCollection<int> ContractorIds { get; init; }

        public QueryProductByFilter(
            int contractorId,
            int categoryId,
            IReadOnlyCollection<int> filterIds,
            IReadOnlyCollection<int> labelIds,
            IReadOnlyCollection<int> warehouseIds,
            IReadOnlyCollection<int> availIds,
            IReadOnlyCollection<ProductQuantityDto> compatibleWithProducts,
            bool? compatibleWithoutWarnings,
            decimal? priceMin,
            decimal? priceMax,
            bool? gifts,
            ProductSort sort,
            bool? warehouses = true,
            bool? additionalServices = false,
            bool? priceIn = false,
            bool? priceInIncludeReserve = false,
            int skip = 0,
            int take = 100,
            bool? sortByAvail = true,
            string pattern = null,
            int[] contractorIds = null,
            int? searchAdditionalServiceProvideProductsByProductId = null,
            int[] productTypeIds = null,
            int[] cartProductIds = null,
            bool useElastic = false)
            : base(
                new QueryProductByFilterRequest(
                    contractorId,
                    categoryId,
                    filterIds,
                    labelIds,
                    warehouseIds,
                    availIds,
                    compatibleWithProducts,
                    compatibleWithoutWarnings,
                    priceMin,
                    priceMax,
                    gifts,
                    warehouses,
                    additionalServices,
                    priceIn,
                    priceInIncludeReserve,
                    skip,
                    take,
                    sort,
                    sortByAvail,
                    pattern,
                    contractorIds,
                    searchAdditionalServiceProvideProductsByProductId,
                    productTypeIds,
                    cartProductIds,
                    useElastic),
                "products",
                "filter")
        {
            ContractorIds = contractorIds;
        }

        public class QueryProductByFilterRequest
        {
            public QueryProductByFilterRequest(
                int contractorId,
                int categoryId,
                IReadOnlyCollection<int> filterIds,
                IReadOnlyCollection<int> labelIds,
                IReadOnlyCollection<int> warehouseIds,
                IReadOnlyCollection<int> availIds,
                IReadOnlyCollection<ProductQuantityDto> compatibleWithProducts,
                bool? compatibleWithoutWarnings,
                decimal? priceMin,
                decimal? priceMax,
                bool? gifts,
                bool? warehouses,
                bool? additionalServices,
                bool? priceIn,
                bool? priceInIncludeReserve,
                int skip,
                int take,
                ProductSort sort,
                bool? sortByAvail = true,
                string pattern = null,
                int[] contractorIds = null,
                int? searchAdditionalServiceProvideProductsByProductId = null,
                int[] productTypeIds = null,
                int[] cartProductIds = null,
                bool useElastic = false)
            {
                CategoryId = categoryId;
                FilterIds = filterIds;
                LabelIds = labelIds;
                WarehouseIds = warehouseIds;
                AvailIds = availIds;
                CompatibleWithProducts = compatibleWithProducts;
                CompatibleWithoutWarnings = compatibleWithoutWarnings;
                ShowArchive = true;
                PriceMin = priceMin;
                PriceMax = priceMax;
                Skip = skip;
                Take = take;
                ContractorId = contractorId;
                Warehouses = warehouses;
                Gifts = gifts;
                AdditionalServices = additionalServices;
                AdditionalServiceProducts = additionalServices;
                PriceIn = priceIn;
                PriceInIncludeReserve = priceInIncludeReserve;
                Sort = sort;
                SortByAvail = sortByAvail;
                Pattern = pattern;
                ContractorIds = contractorIds;
                SearchAdditionalServiceProvideProductsByProductId = searchAdditionalServiceProvideProductsByProductId;
                ProductTypeIds = productTypeIds;
                CartProductIds = cartProductIds;
                UseElasticForProductsSearch = useElastic;
            }

            [JsonProperty("category_id")]
            public int CategoryId { get; init; }

            [JsonProperty("pattern")]
            public string Pattern { get; init; }

            [JsonProperty("contractor_ids")]
            public int[] ContractorIds { get; init; }

            [JsonProperty("filters")]
            public IReadOnlyCollection<int> FilterIds { get; init; }

            [JsonProperty("labels")]
            public IReadOnlyCollection<int> LabelIds { get; init; }

            [JsonProperty("avail_warehouses")]
            public IReadOnlyCollection<int> WarehouseIds { get; init; }

            [JsonProperty("avails")]
            public IReadOnlyCollection<int> AvailIds { get; init; }

            [JsonProperty("show_archive")]
            public bool? ShowArchive { get; init; }

            [JsonProperty("price_min_uah")]
            public decimal? PriceMin { get; init; }

            [JsonProperty("price_max_uah")]
            public decimal? PriceMax { get; init; }

            [JsonProperty("compatible_with_products")]
            public IReadOnlyCollection<ProductQuantityDto> CompatibleWithProducts { get; init; }

            [JsonProperty("compatible_without_warnings")]
            public bool? CompatibleWithoutWarnings { get; init; }

            [JsonProperty("skip")]
            public int Skip { get; init; }

            [JsonProperty("take")]
            public int Take { get; init; }

            [JsonProperty("contractor_id")]
            public int ContractorId { get; init; }

            [JsonProperty("warehouses")]
            public bool? Warehouses { get; init; }

            [JsonProperty("gifts")]
            public bool? Gifts { get; init; }

            [JsonProperty("additional_services")]
            public bool? AdditionalServices { get; init; }

            [JsonProperty("additional_service_products")]
            public bool? AdditionalServiceProducts { get; init; }

            [JsonProperty("price_in")]
            public bool? PriceIn { get; init; }

            [JsonProperty("price_in_include_reserve")]
            public bool? PriceInIncludeReserve { get; init; }

            [JsonProperty("sort")]
            [JsonConverter(typeof(StringEnumConverter))]
            public ProductSort Sort { get; init; }

            [JsonProperty("sort_by_avail")]
            public bool? SortByAvail { get; set; }

            [JsonProperty("search_additional_service_provide_products_by_product_id")]
            public int? SearchAdditionalServiceProvideProductsByProductId { get; }

            [JsonProperty("product_type_ids")]
            public int[] ProductTypeIds { get; }

            [JsonProperty("cart_product_ids")]
            public int[] CartProductIds { get; init; }

            [JsonProperty("use_elastic_for_products_search")]
            public bool UseElasticForProductsSearch { get; init; }
        }
    }
}