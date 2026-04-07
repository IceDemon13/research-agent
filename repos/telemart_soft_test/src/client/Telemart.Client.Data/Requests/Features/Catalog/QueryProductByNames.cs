using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryProductByNames : CallActionWithBodyRequestBase<List<ProductSearchResponseDto>, QueryProductByNames.QueryProductByNamesDto>
    {
        public QueryProductByNames(
            int contractorId,
            string[] names,
            int languageId,
            bool queryGifts,
            int[] categoryIds = null,
            int? priceId = null,
            bool? complectation = null,
            bool? additionalServices = null,
            string[] tags = null,
            int[] cartProductIds = null,
            int skip = 0,
            int take = 10,
            int? precision = null)
            : base(new QueryProductByNamesDto(contractorId, names, categoryIds, priceId, queryGifts, complectation, additionalServices, languageId, tags, cartProductIds, skip, take, precision), "products", "search-by-text")
        {
        }

        public class QueryProductByNamesDto
        {
            public QueryProductByNamesDto(
                int contractorId,
                string[] patterns,
                int[] categoryIds,
                int? priceId,
                bool? gifts,
                bool? complectation,
                bool? additionalServices,
                int language,
                string[] tags,
                int[] cartProductIds,
                int skip,
                int take,
                int? precision)
            {
                ContractorId = contractorId;
                Patterns = patterns;
                CategoryIds = categoryIds;
                PriceId = priceId;
                Gifts = gifts;
                Complectation = complectation;
                AdditionalServices = additionalServices;
                AdditionalServiceProducts = additionalServices;
                Language = language;
                Tags = tags;
                CartProductIds = cartProductIds;
                Skip = skip;
                Take = take;
                Precision = precision;
            }

            [JsonProperty("contractor_id")]
            public int ContractorId { get; set; }

            [JsonProperty("patterns")]
            public string[] Patterns { get; set; }

            [JsonProperty("category_ids")]
            public int[] CategoryIds { get; set; }

            [JsonProperty("price_id")]
            public int? PriceId { get; set; }

            [JsonProperty("gifts")]
            public bool? Gifts { get; set; }

            [JsonProperty("complectation")]
            public bool? Complectation { get; set; }

            [JsonProperty("additional_services")]
            public bool? AdditionalServices { get; set; }

            [JsonProperty("additional_service_products")]
            public bool? AdditionalServiceProducts { get; set; }

            [JsonProperty("language")]
            public int Language { get; set; }

            [JsonProperty("tags")]
            public string[] Tags { get; set; }

            [JsonProperty("cart_product_ids")]
            public int[] CartProductIds { get; set; }

            [JsonProperty("skip")]
            public int Skip { get; set; }

            [JsonProperty("take")]
            public int Take { get; set; }

            [JsonProperty("precision")]
            public int? Precision { get; set; }
        }
    }
}