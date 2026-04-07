using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductByPattern : CallActionWithBodyRequestBase<ProductsPagedResult, object>
    {
        public QueryProductByPattern(
            int contractorId,
            string searchString,
            ProductSort sort,
            int? categoryId,
            bool queryGifts,
            bool excludeDiscounts,
            bool queryAdditionalServices = false,
            bool priceIn = false,
            bool priceInIncludeReserve = false,
            bool includePrices = false,
            int skip = 0,
            int take = 100,
            bool? sortByAvail = true,
            int? searchAdditionalServiceProvideProductsByProductId = null,
            int[] cartProductIds = null,
            int languageId = Language.UkrainianId)
            : base(
                GetBody(
                    contractorId,
                    searchString,
                    sort,
                    categoryId,
                    queryGifts,
                    excludeDiscounts,
                    queryAdditionalServices,
                    priceIn,
                    priceInIncludeReserve,
                    includePrices,
                    skip,
                    take,
                    sortByAvail,
                    searchAdditionalServiceProvideProductsByProductId,
                    cartProductIds,
                    languageId),
                "products",
                "query")
        {
        }

        private static object GetBody(
            int contractorId,
            string searchString,
            ProductSort sort,
            int? categoryId,
            bool queryGifts,
            bool excludeDiscounts,
            bool queryAdditionalServices,
            bool priceIn,
            bool priceInIncludeReserve,
            bool includePrices,
            int skip,
            int take,
            bool? sortByAvail,
            int? searchAdditionalServiceProvideProductsByProductId,
            int[] cartProductIds,
            int languageId)
        {
            return new
            {
                contractor_id = contractorId,
                pattern = searchString,
                category_id = categoryId,
                exclude_discounts = excludeDiscounts,
                allow_barcode = true,
                skip = skip,
                take = take,
                gifts = queryGifts,
                additional_services = queryAdditionalServices,
                additional_service_products = queryAdditionalServices,
                categories = true,
                warehouses = true,
                price_in = priceIn,
                price_in_include_reserve = priceInIncludeReserve,
                include_prices = includePrices,
                ignore_categories = Array.Empty<int>(),
                sort = sort,
                sort_by_avail = sortByAvail,
                search_additional_service_provide_products_by_product_id = searchAdditionalServiceProvideProductsByProductId,
                cart_product_ids = cartProductIds,
                language = languageId,
                search_in_database = true
            };
        }
    }
}